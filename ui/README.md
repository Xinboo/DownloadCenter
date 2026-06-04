# DownloadCenter 前端

ECU 控制器下载中心前端，基于 Vue 3 + TypeScript + Element Plus。

## 技术栈

- Vue 3 + TypeScript
- Vite（开发服务器 + 打包）
- Element Plus（按需引入）
- Pinia（状态管理）
- Vue Router（路由）
- Axios（HTTP 请求）

## 本地开发

```bash
npm install
npm run dev
```

默认运行在 `http://localhost:5173`，已配置代理转发 `/api` 到后端 `http://localhost:5000`。

## 打包构建

```bash
npm run build
```

产出 `dist/` 目录，包含纯静态文件（HTML/JS/CSS），可由任意 HTTP 服务器托管。

## 目录结构

```
src/
├── main.ts            # 应用入口
├── App.vue            # 根组件
├── api/               # 后端接口调用
├── views/             # 页面组件
│   ├── auth/          #   登录、注册管理员
│   ├── admin/         #   管理后台（产品/用户/文件/授权）
│   ├── product/       #   产品中心（用户浏览下载）
│   └── error/         #   404 页
├── layout/            # 主框架布局（顶栏、侧栏、内容区）
├── router/            # 路由配置 + 守卫
├── stores/            # Pinia 状态管理
├── utils/             # 工具函数（axios 封装）
├── types/             # TypeScript 类型定义
├── enums/             # 枚举
├── constants/         # 常量
└── styles/            # 全局样式
```
