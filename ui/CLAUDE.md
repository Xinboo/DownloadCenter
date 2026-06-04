# 前端项目说明（DownloadCenter UI · 截至 2026-06-04）

ECU 控制器下载中心前端。管理员上传产品配套软件/说明书，用户登录后按产品浏览下载。后端进度见仓库根目录 `../CLAUDE.md`。

## 重要约定

- **描述方向统一用「上下左右」**，不要用「垂直 / 水平 / 横向 / 纵向」这类词。
- **思考过程用中文显示**，思考过程要用中文显示，不要用英文显示。

## 技术栈

- Vue 3 + TypeScript + Vite
- vue-router、Pinia
- Element Plus（按需引入，unplugin-vue-components + unplugin-auto-import，含 @element-plus/icons-vue 全局注册）
- axios（封装在 `src/utils/request.ts`）
- 无 Linter / Formatter（ESLint、Prettier、Oxlint 已移除）

## 目录结构

```
src/
├── main.ts              — 应用入口（Pinia / router / 全局图标注册 / ElMessage+ElMessageBox样式 / reset.css）
├── App.vue              — 根组件，el-config-provider(中文语言包) 包裹 <RouterView />
├── router/
│   ├── index.ts         — 路由表 + 引入守卫
│   └── guard.ts         — ✅ 路由守卫（白名单、系统初始化检查、登录检查）+ markSystemInitialized() 导出
├── layout/              — 主框架布局（登录后的壳）
│   ├── LayoutIndex.vue  — el-container：上 header(60px, 黑底+阴影) + 下[左 aside(240px, 仅Admin显示) + 右 main]
│   └── components/
│       ├── LayoutHeader.vue — ✅ 顶栏：左 logo，右用户下拉（真实用户信息 + 修改密码 Dialog + 退出登录）
│       ├── LayoutAside.vue  — ✅ 左侧菜单（按角色动态显示，Admin 5 项 / User 仅产品中心）
│       └── LayoutMain.vue   — <RouterView /> 出口
├── views/
│   ├── auth/
│   │   ├── SetupAdmin.vue   — ✅ 系统初始化建管理员（成功后 markSystemInitialized + 跳登录）
│   │   └── UserLogin.vue    — ✅ 登录页（表单+校验，调 authStore.login，成功跳 /home）
│   ├── admin/
│   │   ├── UserManage.vue   — ✅ 用户管理（分页+搜索+新建/编辑/删除）
│   │   ├── ProductManage.vue — ✅ 产品管理（分页+搜索+CRUD+资源管理Drawer+资源分页）
│   │   ├── AccessManage.vue  — ✅ 产品授权（用户分页+Drawer穿梭框配置+即时保存）
│   │   └── FileManage.vue    — ✅ 文件管理（分页+搜索+上传/下载/预览/删除）
│   ├── product/
│   │   └── ProductHome.vue  — ✅ 产品中心（卡片网格+搜索+分页+详情Drawer+资源分页+下载）
│   └── error/
│       └── NotFound.vue     — ✅ 404 页
├── api/
│   ├── auth.ts          — ✅ login()、refreshToken()
│   ├── user.ts          — ✅ getSystemStatus()、registerAdmin()、changePassword()、getUserPage()、getUserList()、createUser()、updateUser()、deleteUser()
│   ├── product.ts       — ✅ getProductPage()、getProductList()、getProductDetail()、getResourcePage()、createProduct()、updateProduct()、deleteProduct()、publishResource()、deleteResource()
│   ├── file.ts          — ✅ uploadFile()、getFilePage()、deleteFile()
│   └── productAccess.ts — ✅ getAuthorizedProducts()、setAccessAll()、setAuthorizedProducts()
├── types/
│   └── api.ts           — ApiResult<T>、PagedResult<T>
├── enums/
│   └── user.ts          — UserRole（Admin/User）
├── constants/
│   └── storage.ts       — TOKEN_KEY、REFRESH_TOKEN_KEY
├── utils/
│   └── request.ts       — ✅ axios + 拦截器（从 store 读 token，401 自动刷新 + 请求队列）
├── stores/
│   └── auth.ts          — ✅ auth store（token 管理、JWT 解析 userInfo、login/refresh/logout）
└── styles/
    └── reset.css        — 全局重置
```

## 登录流程（已完成）

### auth store（`src/stores/auth.ts`）

- **state**：accessToken / refreshToken（初始从 localStorage 恢复）
- **getters**：
  - `isLoggedIn` — token 是否存在
  - `userInfo` — 从 accessToken 解析 JWT payload（base64 → Uint8Array → TextDecoder 处理中文），兼容多种 claim key 格式（短名/长 URI），提取 userId / userName / nickName / role / canAccessAllProducts
- **actions**：
  - `login(userName, password)` — 调 API，存双 token 到 state + localStorage
  - `refresh()` — 用双 token 调刷新接口，失败则 logout
  - `logout()` — 清 token，动态 import router 跳 `/login`（避免循环依赖）

### 路由守卫（`src/router/guard.ts`）

- 白名单：`/login`、`/setup`（无需登录）
- 首次访问检查系统初始化状态（调 `getSystemStatus()`，结果缓存，只查一次）
  - 未初始化 → 跳 `/setup`
- 非白名单页面：未登录 → 跳 `/login`
- 导出 `markSystemInitialized()`：SetupAdmin 注册成功后调用，更新缓存，避免跳转被拦回 `/setup`

### 401 自动刷新（`src/utils/request.ts`）

- 请求拦截器从 authStore 读 token（单一数据源）
- 响应拦截器 401 → 调 authStore.refresh()
  - 成功：用新 token 重发原请求 + 重发队列中排队的请求
  - 失败：logout 跳登录页
  - 刷新锁（isRefreshing）防止并发请求同时触发 refresh

### 顶栏功能（`src/layout/components/LayoutHeader.vue`）

- 从 authStore.userInfo 读取用户名、昵称、角色
- 修改密码 Dialog（旧密码 + 新密码 + 确认密码，成功后 logout 强制重新登录）
- 退出登录按钮

## 布局与角色

- **LayoutIndex.vue**：Admin 显示左侧 Aside（240px），普通用户隐藏 Aside，内容区占满全宽
- **LayoutAside.vue**：灰色底（#545c64）+ 白色文字 + 蓝色选中（#409eff）
- **LayoutHeader.vue**：黑色底（#303030）+ 下方阴影
- 所有分页条通过 flex `margin-top: auto` 贴在内容区底部

## 左侧菜单（仅 Admin 显示）

| 菜单项 | 路由 | Admin | User |
|---|---|---|---|
| 产品中心 | `/home` | ✅ | 无 Aside |
| ▸ 产品管理（一级） | — | ✅ | — |
| 　产品列表 | `/admin/product` | ✅ | — |
| 　产品授权 | `/admin/access` | ✅ | — |
| 用户管理 | `/admin/user` | ✅ | — |
| 文件管理 | `/admin/file` | ✅ | — |

## 页面功能

### 产品中心（`src/views/product/ProductHome.vue`，已完成）

- **卡片网格**：CSS Grid 响应式（5/4/3/2列），封面图 + 名称/型号/描述/资源数
- **搜索+分页**：产品名称/型号筛选，调 `getProductPage()`
- **详情 Drawer**：点击卡片打开，封面大图 + 产品信息 + 资源文件分页列表（调 `getResourcePage()`，支持文件名搜索）+ 下载按钮
- 普通用户无 Aside，无页面标题，直接展示搜索栏和卡片

### 用户管理（`src/views/admin/UserManage.vue`，已完成）

- **分页+搜索**：用户名/昵称筛选，调 `getUserPage()`
- **新建用户**：Dialog 表单，调 `createUser()`
- **编辑用户**：复用同一 Dialog，编辑模式隐藏用户名和密码字段，调 `updateUser()`
- **删除用户**：ElMessageBox.confirm 二次确认，调 `deleteUser()`

### 产品管理（`src/views/admin/ProductManage.vue`，已完成）

- **分页+搜索**：产品名称/型号筛选，调 `getProductPage()`
- **新建/编辑产品**：Dialog 表单（名称/型号/描述/封面上传），调 `createProduct()` / `updateProduct()`
- **删除产品**：二次确认，调 `deleteProduct()`
- **资源管理 Drawer**：资源分页列表（调 `getResourcePage()`，支持文件名搜索）+ 发布资源 Dialog（上传文件+填写显示名和描述）+ 下载/删除

### 产品授权（`src/views/admin/AccessManage.vue`，已完成）

- **用户分页表格**：用户名/昵称搜索，每行有「配置授权」按钮
- **配置 Drawer（680px）**：查看全部产品 Switch + el-transfer 穿梭框（260px 面板，500px 列表高度，搜索过滤）
- 即时保存：Switch 切换立即调 `setAccessAll()`，穿梭框移动立即调 `setAuthorizedProducts()`

### 文件管理（`src/views/admin/FileManage.vue`，已完成）

- **分页+搜索**：文件名筛选，调 `getFilePage()`
- **上传文件**：拖拽上传 Dialog，调 `uploadFile()`
- **下载/预览/删除**：图片可预览（ElImageViewer），调 blob 下载

## 网络层（`src/utils/request.ts`）

- axios 实例：`baseURL = import.meta.env.VITE_API_BASE`，超时 15s
- **请求拦截器**：从 authStore.accessToken 读 token，自动加 `Authorization: Bearer <token>`
- **响应拦截器**（适配后端「业务失败也返回 HTTP 200」的 ApiResult 格式）：
  - `success === true` → 返回解包后的 `res.data`
  - `success === false` → ElMessage.error 弹提示并 reject
  - HTTP 401 → 自动刷新 token 并重发
  - 其余 HTTP 错误 → 弹后端 message
- **泛型封装** `request<T>(config)` → `Promise<T>`

## 环境与代理

- `.env.development` / `.env.production` 均 `VITE_API_BASE=/api`
- `env.d.ts` 声明了 `ImportMetaEnv.VITE_API_BASE` 类型
- 开发：vite proxy `/api` → `http://localhost:5000`（后端路由本身含 `/api`，不 rewrite）
- 生产：由反向代理转发 `/api`

## 路由表（`src/router/index.ts`）

| 路径 | 组件 | 说明 |
|---|---|---|
| `/login` | UserLogin | 登录页（白名单） |
| `/setup` | SetupAdmin | 系统初始化建管理员（白名单） |
| `/` → `/home` | LayoutIndex > ProductHome | 主框架，重定向到首页 |
| `/admin/product` | ProductManage（懒加载） | 产品管理（占位） |
| `/admin/user` | UserManage（懒加载） | 用户管理 |
| `/admin/access` | AccessManage（懒加载） | 产品授权（占位） |
| `/admin/file` | FileManage（懒加载） | 文件管理（占位） |
| `/:pathMatch(.*)*` | NotFound（懒加载） | 404 |

## 已对接的后端接口

| 前端函数 | 后端接口 | 使用位置 |
|---|---|---|
| `login(data)` | `POST /api/auth/login` | authStore.login → UserLogin.vue |
| `refreshToken(data)` | `POST /api/auth/refresh` | authStore.refresh → request.ts 401 拦截 |
| `getSystemStatus()` | `GET /api/user/status` | 路由守卫 |
| `registerAdmin(data)` | `POST /api/user/register-admin` | SetupAdmin.vue |
| `changePassword(data)` | `PUT /api/user/change-password` | LayoutHeader.vue 修改密码 |
| `getUserPage(params)` | `GET /api/user` | UserManage.vue / AccessManage.vue 分页列表 |
| `getUserList(params)` | `GET /api/user/list` | （不分页，待用） |
| `createUser(data)` | `POST /api/user` | UserManage.vue 新建 |
| `updateUser(id, data)` | `PUT /api/user/{id}` | UserManage.vue 编辑 |
| `deleteUser(id)` | `DELETE /api/user/{id}` | UserManage.vue 删除 |
| `getProductPage(params)` | `GET /api/product` | ProductHome.vue / ProductManage.vue 分页列表 |
| `getProductList(params)` | `GET /api/product/list` | AccessManage.vue 穿梭框 |
| `getProductDetail(id)` | `GET /api/product/{id}` | （产品详情，不含资源列表） |
| `getResourcePage(pid, params)` | `GET /api/product/{id}/resource` | ProductHome.vue / ProductManage.vue 资源分页 |
| `createProduct(data)` | `POST /api/product` | ProductManage.vue |
| `updateProduct(id, data)` | `PUT /api/product/{id}` | ProductManage.vue |
| `deleteProduct(id)` | `DELETE /api/product/{id}` | ProductManage.vue |
| `publishResource(pid, fid, data)` | `POST /api/product/{id}/resource/{fileId}` | ProductManage.vue |
| `deleteResource(pid, fid)` | `DELETE /api/product/{id}/resource/{fileId}` | ProductManage.vue |
| `getFilePage(params)` | `GET /api/file` | FileManage.vue |
| `uploadFile(file)` | `POST /api/file/upload` | FileManage.vue |
| `deleteFile(id)` | `DELETE /api/file/{id}` | FileManage.vue |
| `getAuthorizedProducts(userId)` | `GET /api/product-access/{userId}/products` | AccessManage.vue |
| `setAccessAll(userId, flag)` | `PUT /api/product-access/{userId}/access-all` | AccessManage.vue |
| `setAuthorizedProducts(userId, ids)` | `PUT /api/product-access/{userId}/products` | AccessManage.vue |

## 已完成功能总览

所有前端页面已完成开发：登录/注册、产品中心（卡片浏览+详情下载）、产品管理（CRUD+资源发布）、产品授权（穿梭框配置）、用户管理（CRUD）、文件管理（上传/下载/预览/删除）。
