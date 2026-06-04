# DownloadCenter 开发进度（截至 2026-06-04）

## 项目概述

ECU 控制器下载中心，管理员上传产品配套软件和说明书，用户登录后按产品浏览下载。

## 技术栈

- .NET 8 / ASP.NET Core Web API
- PostgreSQL + EF Core（Npgsql 8.x）
- Clean Architecture 分层：Api / Application / Domain / Infrastructure / Shared
- 已启用 `Npgsql.EnableLegacyTimestampBehavior`，时间用 `DateTime.Now`

## 基础设施（已完成）

### 雪花ID
- `Yitter.IdGenerator`（Admin.NET 同款），Shared 层 `Snowflake/SnowflakeSetup.cs`
- Program.cs 启动时 `SnowflakeSetup.Init()`，workerId=1

### 实体基类
- `Domain/Common/EntityBase.cs`：Id(long), CreatedAt, UpdatedAt?, IsDeleted
- AppDbContext 自动填充审计字段、拦截删除为软删除、全局查询过滤 `!IsDeleted`

### 枚举处理
- API 层 Newtonsoft `StringEnumConverter`，枚举用字符串传输
- EF Core `OnModelCreating` 全局遍历枚举属性，`EnumToStringConverter` 存为字符串

### 对象映射
- `Mapster 10.0.7` + `Mapster.DependencyInjection`，Application 层 DI 扫描注册

### JSON 序列化
- `Newtonsoft.Json`（非 System.Text.Json）
- 驼峰命名、日期格式 yyyy-MM-dd HH:mm:ss、忽略循环引用、忽略 null、枚举字符串

### 统一响应格式
- `Shared/Models/ApiResult.cs`
- `Ok()` / `Ok<T>(data)` — Code=200, Success=true（业务成功）
- `Fail(message)` — Code=200, Success=false（业务失败，如密码错误）
- `Error(message, code)` — Code=500, Success=false（仅全局异常中间件使用）

### 全局异常中间件
- `Api/Middlewares/GlobalExceptionMiddleware.cs`
- 开发环境返回 ex.Message，生产环境返回"服务器内部错误"

### DI 注册入口
- `Infrastructure/DependencyInjection.cs`：注册 DbContext（Npgsql）、JWT Authentication、Authorization、FileStorageOptions
- `Application/DependencyInjection.cs`：注册 Mapster、AuthService、UserService、FileService、ProductService、ProductAccessService
- `Api/Services/CurrentUserService.cs`：注册 HttpContextAccessor + CurrentUser（Scoped）
- `Api/Program.cs`：Kestrel 100MB 限制、FormOptions、Newtonsoft JSON、Swagger Bearer、CORS、自动迁移、静态文件托管、调用上述三个 DI 入口

### EF Configuration（`Infrastructure/Persistence/Configurations/`）
- `UserConfiguration` — UserName 最大50 + 唯一索引，PasswordHash 最大200
- `FileRecordConfiguration` — OriginalName 最大256、StoredName 最大100、Extension 最大20、RelativePath 最大256
- `ProductConfiguration` — Name 最大100、Model 最大100、Description 最大500
- `ProductFileConfiguration` — ProductId+FileId 联合唯一索引，DisplayName 最大200、Description 最大1000，外键级联删除
- `UserProductConfiguration` — UserId+ProductId 联合主键，外键级联删除
- `UserProductAccessConfiguration` — UserId 唯一索引，外键级联删除

## 约束与规范

- 不用 Repository 模式，Service 直接注入 AppDbContext
- Service 不写 IService 接口
- DI 用 .NET 自带容器，不用 Autofac
- 时间用 DateTime.Now，不用 UTC
- 文件管理模块完全独立，不感知任何业务。只能业务关联文件，不能文件关联业务
- User 模块保持纯净，不挂业务字段（CanAccessAllProducts 已移至产品模块独立表 UserProductAccess）

## 功能模块

### 1. 用户模块（已完成）

**实体**：`Domain/Entities/User.cs`
- UserName, PasswordHash, Role(枚举 Admin/User), NickName(不可空), Company?, Phone?

**EF 配置**：UserName 最大50 + 唯一索引，PasswordHash 最大200

**UserService**（`Application/Users/UserService.cs`）：
- `GetSystemStatusAsync()` — 检测是否已有用户（首次运行引导）
- `RegisterAdminAsync(request)` — 仅无用户时可注册管理员
- `GetPageAsync(UserPageInput)` — 用户分页列表（支持 UserName/NickName 筛选）
- `GetListAsync(UserListInput)` — 用户不分页列表（支持 UserName/NickName 筛选）
- `CreateAsync(request)` — 管理员创建用户（无公开注册）
- `DeleteAsync(id)` — 删除用户
- `ChangePasswordAsync(userId, request)` — 修改密码（BCrypt 哈希）
- `UpdateAsync(id, request)` — 编辑用户信息（Role, NickName, Company, Phone）

**UserController** 8 个接口：
- status、register-admin — 匿名
- change-password — 登录用户
- GetPage、GetList、Create、Update、Delete — Admin

### 2. 认证鉴权（已完成）

**JWT 双 token 无状态**：
- AccessToken（30分钟）：claims 含 UserId, UserName, NickName(GivenName), Role, CanAccessAllProducts
- RefreshToken（7天）：claims 仅含 UserId
- 都是 JWT，不存数据库，共用同一个 SecretKey

**配置**：`appsettings.json` → Jwt 节（SecretKey, Issuer, Audience, 过期时间）
- `Shared/Options/JwtOptions.cs`

**AuthService**（`Application/Auth/AuthService.cs`）：
- `LoginAsync(request)` — 验证密码，生成双 token
- `RefreshTokenAsync(request)` — 从过期 AccessToken 解析用户，验证 RefreshToken，换发新 token

**AuthController** 2 个接口（login, refresh），匿名访问。

**CurrentUser**（`Shared/Auth/CurrentUser.cs`）：
- UserId, UserName, NickName, Role, CanAccessAllProducts, IsAuthenticated
- `Api/Services/CurrentUserService.cs` 从 HttpContext.User.Claims 填充，Scoped 注入
- Application 层 Service 可直接注入 CurrentUser

**Swagger**：已配置 Bearer token 输入框。

### 3. 文件管理模块（已完成，独立模块）

**设计原则**：文件管理完全独立，不感知任何业务模块。整个 FileController 仅 Admin 可访问。普通用户通过产品入口下载文件。

**实体**：`Domain/Entities/FileRecord.cs`
- OriginalName(原始文件名), StoredName(GUID.ext), Extension, RelativePath, Size

**存储**：本地磁盘 `{AppContext.BaseDirectory}/uploads/yyyy/MM/dd/GUID.ext`

**配置**：`appsettings.json` → FileStorage 节
- `Shared/Options/FileStorageOptions.cs`：BasePath="uploads", MaxSizeBytes=100MB, AllowedExtensions（文档/图片/压缩包等）
- Kestrel + FormOptions 已配置 100MB 上传限制

**FileService**（`Application/Files/FileService.cs`）：
- `UploadAsync(file)` — 校验类型大小，GUID 重命名，按日期创建目录，保存文件+数据库，返回 FileId
- `GetPageAsync(FilePageInput)` — 文件分页列表（支持 OriginalName 筛选）
- `GetDownloadAsync(id)` — 返回文件路径和原始名（Controller 用 FileExtensionContentTypeProvider 推断 ContentType）
- `DeleteAsync(id)` — 删除数据库记录 + 磁盘文件

**FileController** 5 个接口（upload/page/download/preview/delete），全部仅 Admin。

### 4. 产品模块（已完成）

**实体**：`Domain/Entities/Product.cs`（继承 EntityBase）
- Name(产品名称，最大100), Model(型号，最大100), Description?(描述，最大500), CoverFileId?(封面图，指向 FileRecord)

**产品资源表**：`Domain/Entities/ProductFile.cs`（继承 EntityBase）
- ProductId, FileId（ProductId+FileId 联合唯一索引）
- DisplayName(不可空，自定义显示名，最大200), Description(不可空，如版本修复内容，最大1000)
- 外键：ProductId → Product, FileId → FileRecord，级联删除

**ProductService**（`Application/Products/ProductService.cs`）：
- 注入 CurrentUser 和 FileService
- `GetPageAsync(ProductPageInput)` — 产品分页列表（含文件数量，支持 Name/Model 筛选），根据用户角色和授权过滤
- `GetListAsync(ProductListInput)` — 产品不分页列表（支持 Name/Model 筛选），根据用户角色和授权过滤
- `GetByIdAsync(id)` — 产品详情（不含资源列表），检查用户授权
- `GetResourcePageAsync(productId, ProductResourcePageInput)` — 产品资源分页列表（支持 DisplayName 筛选），带授权检查
- `CreateAsync(request)` — 创建产品
- `UpdateAsync(id, request)` — 编辑产品
- `DeleteAsync(id)` — 删除产品 + 产品资源 + 封面图（调用 FileService.DeleteAsync）
- `PublishResourceAsync(productId, fileId, request)` — 发布产品资源（带 DisplayName + Description）
- `DeleteResourceAsync(productId, fileId)` — 删除产品资源 + 删除文件（调用 FileService.DeleteAsync）
- `GetFileDownloadAsync(productId, fileId)` — 带授权检查的文件下载
- `GetCoverPreviewAsync(productId)` — 带授权检查的封面图预览

**ProductController** 11 个接口：
- GetPage、GetList、GetById、GetResourcePage、DownloadFile、CoverPreview — 登录用户（Service 层做授权过滤）
- Create、Update、Delete、PublishResource、DeleteResource — Admin

### 5. 产品授权（已完成，独立模块）

**权限逻辑**：
- Admin 角色 → 看到全部产品
- 普通用户 CanAccessAllProducts=true → 看到全部产品
- 普通用户 CanAccessAllProducts=false → 只看到 UserProduct 中授权的产品

**中间表**：`Domain/Entities/UserProduct.cs`（纯中间表，不继承 EntityBase）
- UserId + ProductId（联合主键）
- 外键：UserId → User, ProductId → Product，级联删除

**独立配置表**：`Domain/Entities/UserProductAccess.cs`（继承 EntityBase）
- UserId（唯一索引）+ CanAccessAll(bool)
- 外键：UserId → User，级联删除
- 登录/刷新 token 时写入 JWT claim，管理员修改后用户需重新登录或等 token 过期生效

**ProductAccessService**（`Application/Products/ProductAccessService.cs`）：
- `SetAccessAllProductsAsync(userId, canAccessAll)` — 设置"查看全部产品"开关（操作 UserProductAccess 表）
- `SetAuthorizedProductsAsync(userId, request)` — 全量覆盖授权产品（先删全部再批量插入，传空数组=清空授权）
- `GetAuthorizedProductsAsync(userId)` — 查询用户已授权产品列表

**ProductAccessController** 3 个接口（`api/product-access`），全部 Admin

**授权管控**：
- 普通用户不能直接访问 FileController（403）
- 普通用户通过 ProductController 的 DownloadFile/CoverPreview 访问文件（带授权检查）
- ProductService 内部通过 `GetAccessibleProducts()` 统一过滤
- CanAccessAllProducts 从 CurrentUser（JWT claim）读取，不查库

## DTO 清单

### Auth（`Application/Auth/Dtos/`）
- `LoginRequest` — UserName, Password
- `LoginResponse` — AccessToken, RefreshToken, ExpiresAt
- `RefreshTokenRequest` — AccessToken, RefreshToken

### Users（`Application/Users/Dtos/`）
- `RegisterAdminRequest` — UserName, Password, NickName
- `CreateUserRequest` — UserName, Password, Role, NickName, Company?, Phone?
- `ChangePasswordRequest` — OldPassword, NewPassword, ConfirmPassword
- `UpdateUserRequest` — Role, NickName, Company?, Phone?
- `UserPageInput` — PageNumber, PageSize, UserName?, NickName?
- `UserListInput` — UserName?, NickName?
- `UserDto` — Id, UserName, Role, NickName, Company?, Phone?, CreatedAt

### Files（`Application/Files/Dtos/`）
- `FilePageInput` — PageNumber, PageSize, OriginalName?
- `FileDto` — Id, OriginalName, Extension, Size, CreatedAt

### Products（`Application/Products/Dtos/`）
- `CreateProductRequest` — Name, Model, Description?, CoverFileId?
- `UpdateProductRequest` — Name, Model, Description?, CoverFileId?
- `PublishResourceRequest` — DisplayName, Description
- `SetAuthorizedProductsRequest` — ProductIds(List\<long\>)
- `ProductPageInput` — PageNumber, PageSize, Name?, Model?
- `ProductListInput` — Name?, Model?
- `ProductDto` — Id, Name, Model, Description?, CoverFileId?, FileCount, CreatedAt
- `ProductResourcePageInput` — PageNumber, PageSize, DisplayName?
- `ProductDetailDto` — Id, Name, Model, Description?, CoverFileId?, CreatedAt
- `ProductFileDto` — FileId, DisplayName, Description, Extension, Size, CreatedAt

## 全部接口权限总览

| 控制器 | 接口 | 权限 |
|--------|------|------|
| AuthController | login, refresh | 匿名 |
| UserController | status, register-admin | 匿名 |
| UserController | change-password | 登录用户 |
| UserController | GetPage, GetList, Create, Update, Delete | Admin |
| FileController | 全部 5 个 | Admin |
| ProductController | GetPage, GetList, GetById, GetResourcePage, DownloadFile, CoverPreview | 登录用户 |
| ProductController | Create, Update, Delete, PublishResource, DeleteResource | Admin |
| ProductAccessController | SetAccessAll, GetAuthorizedProducts, SetAuthorizedProducts | Admin |

## 开源与部署准备（已完成）

### 敏感信息处理
- `appsettings.json` 已替换为占位符（数据库连接串、JWT SecretKey）
- `appsettings.Development.json` 放真实配置，已加入 `.gitignore` 不提交
- 运行时 Development.json 覆盖 appsettings.json 的同名配置

### CORS
- Program.cs 已加 `AddCors` + `UseCors`，允许所有来源（`AllowAnyOrigin`）
- 项目开源给任何人部署，不写死域名

### 自动迁移
- Program.cs 启动时自动执行 `db.Database.Migrate()`，首次部署自动建表

### Docker 部署方案（已完成）
- 方案：后端 Kestrel 直接托管前端静态文件（UseDefaultFiles + UseStaticFiles + MapFallbackToFile）
- 一个 Docker 镜像包含前后端，一个进程，不需要 Nginx
- Dockerfile 多阶段构建：Node 编译前端 → .NET SDK 编译后端 → 运行时镜像
- docker-compose.yml 映射 appsettings.json + uploads 持久化 + favicon.ico 可选替换
- 容器名 download-center，镜像名 xinboo/download-center
- 已创建文件：Dockerfile、.dockerignore、docker-compose.yml
- 已发布到 Docker Hub：xinboo/download-center

## 数据库迁移历史

1. `InitialCreate` — 全部 6 张表（Users, FileRecords, Products, ProductFiles, UserProducts, UserProductAccesses）
