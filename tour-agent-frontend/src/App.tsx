import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { LandingPage } from './pages/LandingPage';
import { AdminConversationsPage } from './pages/AdminConversationsPage';
import { AdminEscalationsPage } from './pages/AdminEscalationsPage';
import { AdminTelegramPage } from './pages/AdminTelegramPage';

import { AdminProductsPage } from './pages/AdminProductsPage';
import { AdminOffersPage } from './pages/AdminOffersPage';
import { AdminContactsPage } from './pages/AdminContactsPage';
import { AdminOrdersPage } from './pages/AdminOrdersPage';
import { AdminLlmSettingsPage } from './pages/AdminLlmSettingsPage';

/**
 * Root Router của hệ thống phân tách độc lập các tuyến đường Landing Page và Admin Portal,
 * kế thừa và ánh xạ 1-1 theo chuẩn định tuyến của dự án PHP.
 */
export const App: React.FC = () => {
  return (
    <BrowserRouter>
      <Routes>
        {/* Phân hệ Client: Landing Page bán hàng & AI Sakura Consultation */}
        <Route path="/" element={<LandingPage />} />
        <Route path="/tour/:slug" element={<LandingPage />} />

        {/* Phân hệ Admin: Tách riêng từng route nghiệp vụ, không gom chung */}
        <Route path="/admin/products" element={<AdminProductsPage />} />
        <Route path="/admin/offers" element={<AdminOffersPage />} />
        <Route path="/admin/contacts" element={<AdminContactsPage />} />
        <Route path="/admin/orders" element={<AdminOrdersPage />} />

        <Route path="/admin/conversations" element={<AdminConversationsPage />} />
        <Route path="/admin/escalations" element={<AdminEscalationsPage />} />
        <Route path="/admin/channels/telegram" element={<AdminTelegramPage />} />
        <Route path="/admin/telegram" element={<Navigate to="/admin/channels/telegram" replace />} />
        <Route path="/admin/settings/llm" element={<AdminLlmSettingsPage />} />
        <Route path="/admin/llm" element={<Navigate to="/admin/settings/llm" replace />} />

        {/* Điều hướng mặc định của /admin sang trang Sản phẩm */}
        <Route path="/admin" element={<Navigate to="/admin/products" replace />} />

        {/* Redirect mặc định */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
};

export default App;
