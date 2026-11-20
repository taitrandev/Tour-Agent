import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

// Đã tắt StrictMode để ngăn chặn React chạy double-mount (gọi trùng lặp 2 lần API ở môi trường dev)
createRoot(document.getElementById('root')!).render(
  <App />
)
