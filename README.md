# CampusShop 校园商城（教学项目）

“AI 辅助软件测试”课程的教学项目：一个小型校园商城，提供功能完全相同的 **C# 版**和 **Java 版**，任选一个即可。

> 这个项目里**故意留有缺陷**。课程的任务之一，就是用测试把它们找出来。

## 目录结构

```text
campusshop/
├─ docs/
│  └─ requirements.md      需求说明（测试的期望值以它为准）
├─ csharp/                 C# 版（.NET 10）
│  ├─ src/CampusShop/        业务代码：计价、优惠券、下单、登录
│  ├─ src/CampusShop.Web/    网站：网页 + REST API
│  └─ tests/CampusShop.Tests/  单元测试（xUnit）
└─ java/                   Java 版（JDK 21 + Spring Boot）
   ├─ src/main/java/campusshop/      业务代码
   ├─ src/main/java/campusshop/web/  网站：网页 + REST API
   └─ src/test/java/campusshop/      单元测试（JUnit 5）
```

## 运行测试

**C# 版**（需要 .NET 10 SDK）：

```powershell
cd csharp
dotnet test
```

**Java 版**（需要 JDK 21；不需要单独安装 Maven，第一次运行会自动下载）：

```powershell
cd java
.\mvnw test
```

看到 `Passed!`（C#）或 `BUILD SUCCESS`（Java）就说明环境配置成功。

## 启动网站

| 版本 | 命令 | 地址 |
|---|---|---|
| C# | `dotnet run --project csharp/src/CampusShop.Web` | http://localhost:5000 |
| Java | `cd java` 后运行 `.\mvnw spring-boot:run` | http://localhost:8080 |

演示账号：`alice`、`bob`、`carol`，密码都是 `123456`。
接口文档：C# 版 http://localhost:5000/swagger ，Java 版 http://localhost:8080/swagger-ui.html 。

网站的数据保存在内存中，**重启网站后恢复为初始数据**。

## 分支约定

每周有两个分支：

| 分支 | 用途 |
|---|---|
| `weekNN-start` | 第 NN 周上机的起点 |
| `weekNN-solution` | 第 NN 周的参考答案 |

```powershell
git switch week05-start      # 切到第 5 周的起点
```

进度落后时，直接切到下一周的 `start` 分支即可，不会被前面的进度卡住。
切换分支前，先提交或撤销自己的改动（`git status` 应显示 `nothing to commit`）。

## 学生须知

- **课程仓库**：https://gitcode.com/jjggjj521/campusshop.git ，克隆：`git clone https://gitcode.com/jjggjj521/campusshop.git`
- **环境**：C# 需要 .NET 10 SDK；Java 需要 JDK 21（不需要单独装 Maven，用 `.\mvnw`）。
- **切换分支前**：先运行 `git status`，确认没有未提交的改动（应显示 `nothing to commit`）。改动请先提交，或用 `git stash` 暂存。
- **Java 切换分支后**，请用 `.\mvnw clean test`，不要只用 `test`。旧分支编译出来的类会残留，导致莫名其妙的测试失败。
- **不要把密钥、API Key、密码提交进仓库**。一旦推送到远程，就要视为已经泄露。
- **网站数据在内存中**，重启网站就恢复为初始数据，测试前不必清理。
- 遇到问题，先看下面的“常见问题”，仍然解决不了再问老师。

## 常见问题

- **`.\mvnw` 下载很慢**：Maven 默认从国外仓库下载。可在 `%USERPROFILE%\.m2\settings.xml` 中配置阿里云镜像（见课程资料）。
- **端口被占用**：先关闭之前启动的网站（在其终端按 `Ctrl+C`）。
- **`.\mvnw` 提示找不到 Java 或 `JAVA_HOME`**：安装 JDK 21 后，重新打开终端，运行 `java -version` 确认版本。
