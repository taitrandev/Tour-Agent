Source (.NET Web API)
 → Dockerfile → build image
 → push lên Azure Container Registry (ACR)
 → Container Apps Environment
 → Container App (kéo image từ ACR, ingress HTTPS, scale rule)
 → CI/CD (GitHub Actions / Azure DevOps) tự động lặp lại các bước trên

---

### 1. việc publish ở local có giống với build image docker ko?

giống 1 phần. bên trong docker build vẫn chạy những lệnh dotnet restore, dotnet publish mà mình dùng ở local

ý chính: build ở local tạo ra món ăn. build image là tạo ra hộp cơm đóng gói sẵn, gồm món ăn, bát đũa. ai nhận hộp cơm cũng ăn được ngay, không cần có bếp riêng

---

# Quá trình hiểu DockerFile

* **Các sai lầm tránh mắc lại**

## **Bối cảnh**: Yêu cầu kiểm tra file có chỗ nào sai không

* được cho biết về các rủi ro: thiếu file .dockerignore. biên dịch 2 lần là run và publish dẫn đến CI bị chậm, nên giải quyết bằng cách xóa build giữ lại publish
* được cho biết những phần đã đúng: thứ tự copy .csproj => rồi mới đến restore để tận dụng cache

---

## **Bối cảnh:** Yêu cầu giải thích từng dòng code trong file DockerFile để hiểu

Hình dung tổng thể. Hãy tưởng tượng có **2 cái máy tính ảo**:

- **Máy A, xưởng (SDK, ~800MB):** có đủ công cụ để biên dịch code. Dùng xong thì **vứt đi**.
- **Máy B, máy chạy thật (aspnet runtime, ~220MB):** chỉ chạy được app, không biên dịch được. Đây là **image cuối cùng**.

```
Máy local của bạn
   │  docker build
   ▼
[Stage build/publish: máy A] ──copy code vào──► restore ──► publish ──► /app/publish (các file .dll)
                                                                  │
                                                     chỉ copy kết quả
                                                                  ▼
[Stage base/final: máy B] ─────────────────────────► /app/*.dll ──► dotnet TourAgent.Api.dll
                                                                  ▲
                                                     image bạn chạy chỉ có phần này
```

Lợi ích: image cuối nhẹ, không chứa source code hay công cụ build, nên bề mặt tấn công nhỏ hơn.

---

### Stage 1: `base`, khung của máy chạy thật

| Dòng                                                 | Ý nghĩa                                                                                                                                                                              |
| ----------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base` | Lấy image của Microsoft đã cài sẵn .NET 10 runtime (chỉ để chạy, không build được). Đặt tên stage này là `base` để dùng lại sau.                              |
| `USER $APP_UID`                                     | Chuyển sang user thường (UID 1654, có sẵn trong image), không chạy bằng root. Nếu app bị hack thì kẻ tấn công cũng không có quyền root.                              |
| `WORKDIR /app`                                      | Tạo thư mục `/app` và `cd` vào đó.                                                                                                                                          |
| `EXPOSE 8080`                                       | **Chỉ là tài liệu**, báo rằng app nghe ở cổng 8080. Dòng này **không** tự mở cổng ra máy bạn. Việc mở cổng do `-p` lúc `docker run` quyết định. |

Lưu ý: stage này **chưa chạy gì cả**. Nó chỉ là cái khung, đến stage 4 mới được dùng.

### Stage 2: `build`, xưởng

| Dòng                                               | Ý nghĩa                                                                                                                           |
| --------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| `FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build` | Bật máy A, có đầy đủ SDK (compiler, NuGet).                                                                                  |
| `ARG BUILD_CONFIGURATION=Release`                 | Biến dùng **lúc build**, mặc định là `Release`. Có thể đổi bằng `--build-arg BUILD_CONFIGURATION=Debug`.      |
| `WORKDIR /src`                                    | Làm việc trong thư mục `/src` của container.                                                                                 |
| `COPY ["Directory.Build.props", "./"]`            | Chép file cấu hình chung từ máy bạn vào `/src/`.                                                                           |
| 4 dòng `COPY ... .csproj`                        | **Chỉ chép file .csproj**, chưa chép code. Giải thích ở phần cache bên dưới.                                       |
| `RUN dotnet restore ...`                          | Đọc các .csproj và tải toàn bộ package NuGet về. Đây là bước chậm nhất.                                              |
| `COPY src/ src/`                                  | Giờ mới chép toàn bộ code. `.dockerignore` sẽ lọc bỏ `bin/`, `obj/`, `.env`...                                      |
| `WORKDIR "/src/src/TourAgent.Api"`                | `cd` vào thư mục project Api. Có `src/src` là vì `/src` (workdir) cộng với `src/` (thư mục trong repo của bạn). |

**Tại sao copy .csproj trước? Đây là phần quan trọng nhất.**

Docker cache theo từng dòng. Nếu input của một dòng không đổi, Docker dùng lại kết quả cũ, và khi một dòng thay đổi thì mọi dòng sau nó đều phải chạy lại.

- Bạn sửa 1 file `.cs` → các .csproj không đổi → bước `restore` **dùng cache** (mất ~0 giây) → chỉ chạy lại từ `COPY src/` trở đi.
- Bạn thêm package NuGet → .csproj thay đổi → restore chạy lại. Trường hợp này hợp lý vì đúng là cần tải package mới.

Nếu viết `COPY . .` ngay từ đầu thì mỗi lần sửa 1 dòng code đều phải tải lại toàn bộ package.

### Stage 3: `publish`, đóng gói thành phẩm

| Dòng                               | Ý nghĩa                                                                                                                                                                                                                                                        |
| ----------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `FROM build AS publish`           | Tiếp tục ngay trên máy A, giữ nguyên mọi thứ đã có.                                                                                                                                                                                                   |
| `ARG BUILD_CONFIGURATION=Release` | Phải khai báo lại, vì `ARG` không tự truyền sang stage mới.                                                                                                                                                                                            |
| `RUN dotnet publish ...`          | Biên dịch và gom mọi thứ cần để chạy vào `/app/publish`. `-c Release` để tối ưu code. `/p:UseAppHost=false` để không tạo file thực thi native, vì chạy bằng `dotnet xxx.dll` là đủ. `--no-restore` vì đã restore ở trên. |

### Stage 4: `final`, image thật sự

| Dòng                                          | Ý nghĩa                                                                              |
| ---------------------------------------------- | -------------------------------------------------------------------------------------- |
| `FROM base AS final`                         | Quay về máy B (stage 1). Máy A bị bỏ lại và không nằm trong image cuối.      |
| `WORKDIR /app`                               | Thừa, vì `base` đã đặt rồi. Giữ cũng không sao.                            |
| `COPY --from=publish /app/publish .`         | Lấy **chỉ** thư mục kết quả từ máy A, đặt vào `/app` của máy B.   |
| `ENV ASPNETCORE_HTTP_PORTS=8080`             | Biến môi trường **lúc chạy**: Kestrel nghe ở cổng 8080.                  |
| `ENV ASPNETCORE_ENVIRONMENT=Production`      | App đọc `appsettings.Production.json`. Có thể ghi đè lúc chạy.               |
| `ENTRYPOINT ["dotnet", "TourAgent.Api.dll"]` | Lệnh chạy khi container khởi động. Process này chết thì container cũng dừng. |

**`ARG` khác `ENV` thế nào:** `ARG` chỉ tồn tại lúc build, còn `ENV` tồn tại cả khi container chạy.

---

Đứng ở **thư mục gốc repo** (nơi có `Directory.Build.props`):

```bash
# 1. Build image
docker build -t touragent .

# 2. Chạy container
docker run --rm -p 5000:8080 --env-file .env -e ASPNETCORE_ENVIRONMENT=Development touragent
```

Giải thích lệnh `run`:

- `-p 5000:8080` nghĩa là **cổng máy bạn : cổng trong container**. Mở `http://localhost:5000` thì request sẽ vào app ở cổng 8080.
- `--env-file .env` truyền secret (API key, connection string) lúc chạy. File `.env` **không** nằm trong image, và đó là cách đúng.
- `-e ASPNETCORE_ENVIRONMENT=Development` là tùy chọn, dùng khi bạn muốn bật Swagger hoặc log chi tiết ở local.
- `--rm` xóa container khi bạn dừng nó (Ctrl+C).

### 3 lỗi hay gặp khi chạy local lần đầu

1. **Không kết nối được DB hay Redis ở `localhost`.** Trong container, `localhost` là **chính container đó**, không phải máy bạn. Hãy đổi connection string thành `host.docker.internal` (Mac/Windows; trên Linux thì thêm `--add-host=host.docker.internal:host-gateway`).
2. **Không thấy Swagger.** Thường là do đang ở `Production`, mà Swagger hay chỉ được bật khi ở Development. Dùng cờ `-e` như trên.
3. **App crash ngay lập tức.** Xem log bằng cách chạy không có `-d` (log sẽ hiện thẳng ra terminal), hoặc dùng `docker logs <container_id>`. Nguyên nhân thường là thiếu biến môi trường hoặc config.

### Thử cache để hiểu rõ hơn

1. Chạy `docker build` lần 1 và để ý thời gian của bước restore => thời gian lâu
2. Sửa 1 dòng trong file `.cs` bất kỳ, rồi build lại. Bạn sẽ thấy `CACHED` ở bước `dotnet restore` => thời gian nhanh, vì không phải sửa những file .csproj

---

## Bối cảnh: hỏi về .csproj để Docker cache

**Instruction** là một lệnh trong Dockerfile, tức là một dòng bắt đầu bằng từ khoá như `FROM`, `COPY`, `RUN`. Một instruction có thể viết trên nhiều dòng nhờ dấu `\`, nên gọi là "bước" thì chính xác hơn "dòng".

**Layer** là **phần thay đổi trên file system** mà một bước tạo ra. Nó giống một tấm giấy trong suốt: chỉ ghi những file được thêm, sửa hoặc xoá so với tấm bên dưới.

* Các từ khóa tạo layer: COPY, RUN, ADD, (WORKDIR đc xem là trường hợp biên, nếu thư mục chưa tồn tài thì docker phải tạo, nên sẽ sinh ra 1 layer)
* Các từ khóa không tạo layer: ENV, USER, EXPOSE, ARG, HEALTHCHECK, WORKKDIR,...

**Mục đính chính của việc COPY những file .csproj (Docker Layer Cache):**

* Là cho việc restore sau đó được cache. Các dòng COPY rẻ nhưng restore thì đắt, và nó chỉ đc cache khi input được ổn định
* vì RUN, docker sẽ kiểm tra các chuỗi lệnh không đổi và các layer trước dùng cache. vì vậy những việc này sẽ quyết định restore có được cache hay ko

lấy ví dụ COPY 4 file .csproj để dễ tưởng tượng:

* Quy luật cache: Docker kiểm tra CheckSum từ trên xuống. Gặp bước đầu tiên có thay đổi thì bước đó và mọi bước phía sau đều phải chạy lại. Đây là lý do bạn copy csproj trước: khi bạn chỉ sửa code .cs, cache bị phá từ COPY src/ trở xuống, còn restore (bước chậm nhất) vẫn giữ được cache
* khi sửa file dòng 1 thì tất cả dòng sau mất cache và chạy lại. nếu sửa file dòng 4 thì 3 dòng đầu có cache, dòng 4 chạy lại
* Docker cache theo từng dòng: nghĩa là Docker sẽ checksum của file .csproj. Docker không quan tâm bên trong `.csproj` bạn sửa dòng nào. Với Docker, `.csproj` chỉ là một file: nội dung đổi một ký tự thì checksum đổi và bước COPY file đó mất cache.
* cơ chế cache thật sự:
* * Với COPY, Docker tính checksum nội dung file được copy và so sánh
  * Với RUN, Docker chỉ so sánh chuỗi lệnh và các layer trước
* Khai báo lại ARG BUILD_CONFIGURATION ở stage publish là đúng, vì ARG không được kế thừa sang stage khác.

**Các câu hỏi thêm:**

###### 1. workdir sẽ chỉ tạo folder hay chỉ cd?

   workdir sẽ tạo thư mục nếu chưa có và chuyển đến thư mục

###### 2. sự khác ngau giữa mcr.microsoft.com/dotnet/aspnet:10.0 và mcr.microsoft.com/dotnet/sdk:10.0

* sdk là bộ công cụ phát triển nên dung lượng rất nặng dùng để build, test, publish. bảo mật thấp hơn khi chạy thực tế vì chứa nhiều công cụ CLI, trình biên dịch dễ bị lợi dụng nếu bị tấn công
* Ngược lại aspnet chỉ chứa môi trường runtime nên dung lượng rất nhẹ, dùng để run các ứng dụng đã đc đóng gói thành dll. bảo mật cao hơn vì bề mặt tấn công (attack surface) nhỏ, ko có công cụ biên dịch bên trong, bảo mật cho môi trường PROD
* Trade-off nếu dùng sdk cho stage final image
  * nhược điểm: rủi ro bảo mật. tốc độ deploy. khởi động và tốn nhiều RAM hơn
  * ưu điểm: tiện cho việc debug trực tiếp, trường hợp máy cá nhân ko reproduce đc bug mà cần làm trên PROD

###### 4. Các stage trong docker được phân biệt như thế nào?

* bản chất

  * Mỗi lệnh FROM trong Dockerfile được xem là 1 stage mới, không kế thừa gì từ stage trước nếu ko chủ động lấy
  * chỉ những file COPY --from mới đi tiếp sang stage sau
  * stage cuối hoặc được chỉ định bằng --target mới thành image. các stage khác chỉ tồn tại trong lúc build
* Định danh stage bằng từ khóa AS để dễ quản lý và dễ gọi lại khi cần
* các stage tương tác với nhau thông qua cờ --from trong lệnh copy
* Một điểm dễ hiểu sai, Stage không giống layer.

  * Layer là kết quả của từng lệnh (RUN, COPY...) và cộng dồn vào image. Xóa file ở layer sau thì image vẫn nặng như cũ.
  * Stage là ranh giới cắt bỏ: không mang sang stage sau thì thứ đó thực sự không có trong image cuối.
* ví dụ

  * Lập trình: build frontend (React/Vue)

    * Bước npm install && npm run build tạo ra node_modules vài trăm MB. Thứ cần ship chỉ là thư mục dist/ gồm HTML/JS/CSS tĩnh, đặt vào Nginx. → Stage 1 là Node, stage 2 là Nginx với dist/.
  * Nhà hàng: bếp và bàn ăn

    * Bếp có dao, thớt, vỏ hành, dầu mỡ. Khách chỉ nhận đĩa thức ăn đã trình bày. Không ai bưng cả cái bếp ra bàn. → Bếp là builder stage, đĩa ăn là final stage.
  * Xây nhà: giàn giáo

    * Lúc xây cần giàn giáo, máy trộn bê tông, công nhân. Khi bàn giao, những thứ đó được tháo hết và chủ nhà chỉ nhận ngôi nhà. → Giàn giáo là công cụ build, cần trong quá trình làm nhưng không thuộc về sản phẩm.
  * In sách: bản thảo và cuốn sách

    * Tác giả có bản nháp, ghi chú, các lần sửa, file thiết kế. Nhà in chỉ in bản cuối, và độc giả mua cuốn sách mà không nhận kèm đống bản nháp. → Stage trung gian chứa "lịch sử làm việc", stage cuối chỉ chứa kết quả.

###### 5. Hiểu về Layer trong docker

* có thể hiểu layer trong docker như các tờ giấy trong suốt có ghi lại sự thay đổi của file. khi xếp chồng lên nhau sẽ thấy được bức tranh hoàn chỉnh (tức là docker image)
* Hãy tưởng tượng bạn đang vẽ một bức tranh:
  * Layer 1 (FROM ubuntu): Bạn lấy một tờ giấy trắng vẽ sẵn nền trời xanh (Hệ điều hành Ubuntu cơ bản). Tờ giấy này là chỉ đọc (read-only), bạn không sửa gì được ở đây.
  * Layer 2 (RUN apt-get install python3): Bạn đặt thêm một tờ giấy trong suốt lên trên, vẽ thêm cây cối lên đó (cài thêm Python).
  * Layer 3 (COPY . /app): Bạn đặt tiếp một tờ giấy trong suốt lên trên cùng, vẽ thêm ngôi nhà (copy mã nguồn của bạn vào thư mục /app)
  * Khi bạn nhìn từ trên xuống (Docker Image), bạn thấy cả nền trời, cây cối và ngôi nhà gộp lại thành một hệ thống file thống nhất. Nhưng bản chất chúng nằm rời rạc trên từng lớp giấy chồng lên nhau
* nhóm từ khóa tạo layer dữ liệu (Tăng dung lượng Image)
  * Mỗi khi dùng 3 từ khóa này, Docker sẽ tạo ra một lớp mới lưu trữ các file hoặc thư mục bị thay đổi:
    * RUN: Chạy các câu lệnh terminal (cài đặt phần mềm, tạo thư mục, tải file...). Mọi file mới sinh ra hoặc bị sửa đổi trong bước này đều được đóng gói thành 1 layer.
    * COPY: Sao chép file/thư mục từ máy thật của bạn vào trong Image.
    * ADD: Tương tự như COPY, nhưng có thêm tính năng tự giải nén file .tar và tải file từ URL.
    * (Lưu ý: Lệnh FROM thực chất là lấy toàn bộ các layer có sẵn của Image gốc làm nền tảng ban đầu, chứ không tự tạo ra một layer mới từ con số 0).
* nhóm từ khóa ko tạo layer dữ liệu (Chỉ tạo Metadata)
  * Từ Docker phiên bản 1.10 trở đi, các lệnh dưới đây không tạo ra layer chứa file. Chúng chỉ ghi thêm thông tin cấu hình (metadata) vào file JSON mô tả Image, nên dung lượng Image gần như không đổi (0 bytes):
    * Nhóm thiết lập môi trường & thư mục:
      * WORKDIR: Đặt thư mục làm việc mặc định.
      * ENV: Khai báo biến môi trường.
      * ARG: Khai báo biến dùng trong lúc build.
      * USER: Chỉ định user sẽ dùng để chạy các lệnh tiếp theo.
    * Nhóm cấu hình mạng & lưu trữ:
      * EXPOSE: Khai báo cổng (port) mà container sẽ lắng nghe.
      * VOLUME: Khai báo điểm gắn kết dữ liệu (mount point).
    * Nhóm khởi chạy & thông tin:
      * CMD: Lệnh mặc định chạy khi khởi động container.
      * ENTRYPOINT: Lệnh gốc bắt buộc chạy khi khởi động container.
      * LABEL: Gắn nhãn thông tin (tác giả, phiên bản...).
      * HEALTHCHECK: Cấu hình cách kiểm tra sức khỏe của container.
      * STOPSIGNAL / SHELL: Các cấu hình hệ thống nâng cao.

###### 7. xử lý tình trạng docker dùng lại cache cũ cho dotnet restore và bỏ qua các bản mới của package khi dùng floating version (1.*)

* Dùng package lock file => dự án cần tính ổn định
* dùng biến ARG để ép phá cache => trường hợp bắt buộc giữ floating version
* Tắt cache hoàn toàn bằng --no-cache
* nhưng nếu không dùng floating version mà dùng version cụ thể thì ko cần, vì Docker nhận diện đc sự thay đổi trong file .csproj
  * tuy nhiên vẫn có lý do các dự án giữ lại package lock file là:
    * Kiểm soát các thư viện phụ thuộc ngầm (Transitive Dependencies)

      * dotnet restore (thông thường): Tin tưởng tuyệt đối vào packages.lock.json, tải đúng những gì đang ghi trong đó mà không cần suy nghĩ.
      * dotnet restore --force-evaluate: Xé bỏ packages.lock.json cũ, đọc lại .csproj từ đầu theo đúng nguyên tắc của NuGet (lấy bản thấp nhất với version cố định (Lowest Applicable Version), lấy bản cao nhất với floating version *), sau đó xuất ra một file packages.lock.json mới.
    * Bảo mật chống tấn công chuỗi cung ứng (Package Tampering)
* file package lock có thể xem như là best practice tiêu chuẩn cho các ứng dụng chạy PROD
  * đảm bảo tính nhất quán tuyêt đối: Code chạy trên máy dev, môi trường test và server production dùng chung một bộ thư viện giống hệt nhau đến từng byte.
  * Tối ưu tốc độ và độ an toàn cho Docker. Kết hợp với --locked-mode, bạn vừa tận dụng được cache của Docker cực nhanh, vừa chống được lỗi tải nhầm bản vá mới gây sập hệ thống ngoài ý muốn.
  * Tăng cường bảo mật. Ngăn chặn rủi ro tấn công chuỗi cung ứng (package tampering) nhờ cơ chế kiểm tra mã băm contentHash.
* bộ đôi hoàn hảo nhất được Microsoft và cộng đồng khuyên dùng là:
  * Central Package Management (Directory.Packages.props). Quản lý toàn bộ phiên bản thư viện ở một file duy nhất ở thư mục gốc.
  * Lock File (packages.lock.json). Khóa cứng toàn bộ cây thư viện và tối ưu hóa quá trình build trên Docker/CI/CD.

###### 8. dấu / ở cuối

* COPY src/ src/ => thói quen tốt và để dễ đọc là luôn nên thêm / ở cuối để bảo đảm Docker luôn xem destination là folder

10. sự khác biệt giữa có và không có dấu / ở đầu

* Trong Linux và Docker, dấu gạch chéo / ở đầu đường dẫn quyết định điểm xuất phát của lệnh WORKDIR:
  * Đường dẫn tuyệt đối (Có dấu / ở đầu): Lệnh WORKDIR "/src/TourAgent.Api" bảo Docker quay về thư mục gốc cao nhất của hệ điều hành (root /) rồi mới đi tìm thư mục src. Vì code của bạn nằm trong /app/src/... chứ không phải /src/..., Docker sẽ tạo ra một thư mục trống mới ở ngoài cùng và báo lỗi không tìm thấy project khi build.
  * Đường dẫn tương đối (Không có dấu / ở đầu): Lệnh WORKDIR "src/TourAgent.Api" bảo Docker đi tiếp từ vị trí hiện tại (/app). Kết quả sẽ đưa bạn vào đúng /app/src/TourAgent.Api.
  * best practice là nên dùng 1 kiểu đường dẫn. ưu tiên dùng đường dẫn tuyệt đối. cách này giúp file cấu hình minh bạch, dễ đọc, loại bỏ hoàn toàn nguy cơ lạc thư mục.
  * việc bắt đầu bằng dấu / ở lệnh WORKDIR mang lại nhiều lợi ích
    * Biết chính xác vị trí đang đứng: Khi nhìn vào bất kỳ dòng WORKDIR /app/src/TourAgent.Api nào, bạn biết ngay lập tức mình đang ở đâu trong hệ thống mà không cần phải cuộn chuột lên trên để dò xem các lệnh WORKDIR trước đó đã đi qua những thư mục nào.
    * Tránh lỗi cộng dồn thư mục: Nếu sau này bạn chỉnh sửa hoặc chèn thêm một lệnh WORKDIR ở giữa file, các đường dẫn tuyệt đối phía dưới vẫn hoạt động chính xác mà không bị lệch vị trí như đường dẫn tương đối.
    * Dễ bảo trì cho người khác: Đồng nghiệp khi đọc Dockerfile của bạn sẽ không phải tự nhẩm tính đường dẫn hiện tại là gì.
  * Cách dùng đường dẫn tuyệt đối cho lệnh COPY
    * Khi áp dụng đường dẫn tuyệt đối cho COPY, bạn chỉ cần lưu ý sự khác biệt giữa 2 tham số của lệnh này:
    * Tham số thứ nhất (Nguồn - Source): Là đường dẫn trên máy tính của bạn (tính từ thư mục build context chứa file .sln). Tham số này bắt buộc viết tương đối (ví dụ: src/TourAgent.Api/TourAgent.Api.csproj), không có dấu / ở đầu.
    * Tham số thứ hai (Đích - Destination): Là đường dẫn bên trong Docker container. Bạn có thể viết tuyệt đối bắt đầu bằng dấu / (ví dụ: /src/TourAgent.Api/).

###### 9. hỏi về debug bằng Visual Studio Container Tools

Khi bạn ngồi debug thì gần như giống hệt debug local: nhấn F5, đặt breakpoint, Step Over/Into, xem biến ở Watch/Locals, sửa code bằng Hot Reload. Khác biệt là code không chạy trên Windows của bạn mà chạy trong một container Linux. Visual Studio đứng ngoài và điều khiển debugger nằm trong container. localhost trỏ đến container chứ ko phải máy mình

mục đích thực tế là lỗi chỉ có thể bắt được trên môi trường linux, vd lỗi hoa thường trong đường dẫn file (có thời gian thì xem lại cách setup debug trong container, setup 1 lần nhưng chưa đc)

nếu trên máy windows đã có sẵn máy ảo wsl thì ko cần debug container cũng đc

10. quy ước trong dockerfile

* COPY, WORKDIR: dạng thường, không ngoặc kép: Đường dẫn không có dấu cách thì JSON chỉ thêm nhiễu
* RUN: dạng shell (không dùng JSON): Cần shell để $BUILD_CONFIGURATION được thay giá trị
* ENTRYPOINT/CMD: luôn dạng JSON: Dạng shell bọc process trong /bin/sh, khiến app không nhận được SIGTERM nên không shutdown gracefully (hadolint DL3025)
* Mọi đường dẫn trong container: tuyệt đối. Đích là thư mục thì kết thúc bằng /
* Ngoại lệ duy nhất: đường dẫn có dấu cách thì dùng JSON.

---

1. Cơ chế cache

Tại sao copy .csproj trước? Docker cache theo từng dòng. từng dòng ở đây

1. Thứ tự copy .csproj → restore → copy source để tận dụng cache
2. mỗi dòng trong docker file là 1 layer
3. Stage

Docker phân biệt và nhận diện stage bằng từ khóa FROM. Đặt tên định danh cho mỗi stage bằng từ khóa AS. syntax là: FROM image_name AS stage_name

Mặc dù stage tách biệt hoàn toàn về hệ thống file nhưng vẫn chia sẻ dữ liệu cho nhau bằng cờ --from trong COPY.

* giải thích ví dụ sau: chuyển stage có tên là final => cd vào folder /app => vào stage publish với đường dẫn /app/publish và copy vào folder hiện tại, dấu chấm đại diện cho /app

```
FROM base AS final 
WORKDIR /app
COPY --from=publish /app/publish .
```

## Bối cảnh đang muốn hiểu docker

**Kết luận:** Chỉ cần nắm chắc **6 khái niệm + khoảng 10 lệnh** là đủ để làm được khoảng 80% công việc thực tế, bao gồm cả pipeline .NET → ACR → Container Apps của bạn. Phần còn lại như networking nâng cao, Swarm, tối ưu layer chuyên sâu thì để sau, khi gặp vấn đề thật rồi học.

---

## Phần 1: 6 khái niệm cốt lõi

| # | Khái niệm                                    | Hiểu đơn giản                                                                            | Tại sao quan trọng                                                                |
| - | ---------------------------------------------- | -------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------- |
| 1 | **Image**                                | Khuôn đúc: một bản đóng gói **chỉ đọc** gồm app, runtime và OS tối giản | Đây là thứ bạn build, push lên ACR và deploy                                 |
| 2 | **Container**                            | Một lần chạy của image (giống quan hệ class → object)                                 | Từ một image có thể chạy nhiều container. Đây là thứ Container Apps scale |
| 3 | **Dockerfile**                           | Công thức để tạo image                                                                  | 80% lỗi nằm ở đây                                                              |
| 4 | **Registry** (Docker Hub, ACR)           | Kho chứa image, giống GitHub nhưng dành cho image                                        | Cầu nối giữa nơi build và nơi chạy                                           |
| 5 | **Tag**                                  | Nhãn phiên bản của image (`myapi:1.0`, `myapi:abc123`)                               | Giúp biết đang chạy bản nào và rollback được                              |
| 6 | **Container là tạm thời (ephemeral)** | Container chết là mọi thứ ghi bên trong mất                                            | Đây là nhận thức quan trọng nhất (xem Phần 3)                               |

> Nếu chỉ nhớ một câu: **Dockerfile → (build) → Image → (push) → Registry → (pull + run) → Container**

---

## Phần 2: Khoảng 10 lệnh đủ dùng hằng ngày

```bash
# Build & chạy
docker build -t myapi:1.0 .                # tạo image từ Dockerfile
docker run -d -p 8080:8080 myapi:1.0       # chạy container (-d: chạy nền, -p: map port máy:container)
docker run -e ConnectionStrings__Db="..." myapi:1.0   # truyền biến môi trường

# Quan sát & debug (dùng nhiều nhất khi có lỗi)
docker ps                 # container đang chạy (thêm -a để xem cả container đã chết)
docker logs <id>          # xem log, lệnh debug số 1
docker exec -it <id> sh   # chui vào trong container để kiểm tra

# Quản lý
docker images             # liệt kê image
docker stop <id> / docker rm <id> / docker rmi <image>

# Registry
docker login myacr.azurecr.io
docker tag myapi:1.0 myacr.azurecr.io/myapi:1.0
docker push myacr.azurecr.io/myapi:1.0
```

---

## Phần 3: 5 nhận thức tránh được phần lớn lỗi

Đây là phần quan trọng hơn cả các lệnh, vì người mới hay sai ở tư duy chứ không phải ở cú pháp.

**1. Container không giữ dữ liệu.**
Không lưu file upload, database hay log vào trong container. Dữ liệu cần giữ lại phải để bên ngoài: Azure Blob, SQL, hoặc **volume**. Trên Container Apps, mỗi lần deploy hoặc scale là có container mới, dữ liệu cũ mất.

**2. Cấu hình đưa vào qua biến môi trường, không đóng gói vào image.**
Cùng **một image** chạy được ở dev, staging và production; chỉ khác biến môi trường.

- ❌ Không đưa connection string hoặc API key vào `appsettings.json` rồi build vào image, vì ai pull được image là đọc được bí mật.
- ✅ Dùng biến môi trường hoặc secret của Container Apps / Key Vault.
- Trong .NET, `ConnectionStrings:Db` được viết thành `ConnectionStrings__Db` (hai dấu gạch dưới).

**3. `localhost` trong container là chính container đó.**
App trong container gọi `localhost:5432` sẽ **không** tới được database trên máy bạn. Đây là lỗi kinh điển của người mới. Ngoài ra, app phải lắng nghe trên `0.0.0.0` chứ không phải `127.0.0.1`. Image .NET 8 mặc định đã lắng nghe ở port **8080**, và port này phải khớp với `-p` khi chạy local và với cấu hình ingress của Container Apps.

**4. Thứ tự lệnh trong Dockerfile quyết định cache.**
Lệnh nào ít thay đổi thì đặt lên trên. Đó là lý do Dockerfile ở trên `COPY *.csproj` rồi `restore` trước, sau đó mới `COPY . .`. Sửa code thì không phải tải lại NuGet, build nhanh hơn rất nhiều trong CI/CD. Nhớ thêm file `.dockerignore` (loại `bin/`, `obj/`, `.git`).

**5. Không dùng tag `latest` khi deploy.**
`latest` không có nghĩa là "mới nhất", nó chỉ là một cái tên. Trong CI/CD nên gắn tag bằng **git commit SHA** để biết chính xác đang chạy code nào và rollback bằng cách trỏ về tag cũ.

---

## Phần 4: Tạm bỏ qua lúc này

| Chủ đề                                   | Lý do bỏ qua                                                                                       |
| ------------------------------------------- | ---------------------------------------------------------------------------------------------------- |
| Docker Swarm, Kubernetes                    | Azure Container Apps đã lo phần orchestration                                                     |
| Docker networking nâng cao                 | Container Apps đã lo ingress                                                                       |
| Tối ưu image cực nhỏ (alpine, chiseled) | Chỉ cần khi có vấn đề về chi phí hoặc bảo mật                                             |
| Docker Compose                              | **Ngoại lệ:** nên học sơ khi cần chạy app kèm database ở local, mức ưu tiên thứ 2 |

---

## Cách học hiệu quả nhất: làm, quan sát, điều chỉnh

Bài tập khoảng 1–2 giờ, có thể làm ngay:

1. `dotnet new webapi` → viết Dockerfile như ở trên → `docker build`
2. `docker run -p 8080:8080` → gọi thử API
3. Cố tình làm sai: map sai port, thiếu biến môi trường → đọc `docker logs` để tìm lỗi
4. `docker exec` vào container và xem trong đó có gì
5. Xóa container, chạy lại, rồi kiểm tra xem file đã ghi lúc trước còn không (để thấy rõ nhận thức số 1)

---
