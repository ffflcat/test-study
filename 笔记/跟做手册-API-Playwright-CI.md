# 软件测试三件套 · 跟做手册（零代码版）

> 配套第 09 / 10 / 12 课。**本手册不提供任何测试代码**，代码一律由 AI 按提示词生成。
> 你只做四件事：照着点、照着敲、复制提示词、对照验收标志。

## 怎么用

- 全书只有四种记号：**▶ 你做什么**、**✅ 怎么算过**、**⚠ 卡住时看哪**、**🤖 提示词**。
- 每一步都有 ✅，**没看到 ✅ 不要往下走**，否则后面全是连锁报错。
- 命令不用理解，整行复制到 PowerShell 回车即可。
- 需要写代码的步骤，统一交给 AI：提示词已经写好，连"不许改什么"都写进去了。

## 总览

| 阶段 | 预计时长 | 结束时你手里有 |
| --- | --- | --- |
| 阶段 0 开工检查 | 10 分钟 | 网站能开、文档能下、工具就位 |
| 阶段 A API 测试 | 40 分钟 | 一份场景表 + 一组 API 测试 + 一份越权缺陷报告 |
| 阶段 B Playwright | 60 分钟 | 3 个 E2E 测试 + 一份审查记录 |
| 阶段 C CI | 60 分钟 | 一条绿色流水线 + 一个 PR + 分支保护生效的截图 |

> 端口记住两个：**C# 是 5000，Java 是 8080**。下文所有地址默认 C#，Java 同学自行替换。

---

# 阶段 0 开工检查

**1. 切到本课分支**
- ▶ 在仓库根目录：`git switch week09-start`
- ✅ 终端提示 `Switched to branch 'week09-start'`

**2. 启动网站（这个终端全程别关）**
- ▶ `dotnet run --project csharp/src/CampusShop.Web`
- ✅ 出现 `Now listening on: http://localhost:5000`

**3. 装 REST Client 插件**
- ▶ VS Code 左侧扩展图标 → 搜 `REST Client` → 认准作者 `humao` → 安装
- ✅ 打开任意 `.http` 文件，请求上方出现灰色 `Send Request`

**4. 下载接口文档**
- ▶ 另开一个终端：`New-Item -ItemType Directory -Force docs`，然后 `Invoke-WebRequest http://localhost:5000/swagger/v1/swagger.json -OutFile docs\openapi.json`
- ✅ 打开 `docs/openapi.json`，按 `Ctrl+F` 能搜到 `/api/orders`

**5. 装 PowerShell 7（阶段 B 要用）**
- ▶ `winget install Microsoft.PowerShell`，装完重开终端，敲 `pwsh -v`
- ✅ 显示 `PowerShell 7.x`

**6. 登录 GitHub CLI（阶段 C 要用）**
- ▶ `gh auth status`
- ✅ 显示 `Logged in to github.com as 你的用户名`；若没登录，执行 `gh auth login` 走浏览器授权

**7. 确认 Playwright MCP 可用**
- ▶ 在 AI 工具里问一句："你现在能用 Playwright 打开网页吗？"
- ✅ AI 回答可以；不行就回到第 04 课重装 MCP

---

# 阶段 A API 测试

## A-1 用 Swagger 手动调三个接口

**1. 打开接口文档页**
- ▶ 浏览器访问 `http://localhost:5000/swagger`
- ✅ 看到按 `auth / products / cart / orders` 分组的接口列表

**2. 调登录，拿 Token**
- ▶ 展开 `POST /api/auth/login` → 点 `Try it out` → 请求体填 `{"username":"alice","password":"123456"}` → 点 `Execute`
- ✅ 响应码 `200`，响应体里有一大串 `eyJ` 开头的 token
- ▶ 把 token 整串复制下来（不要带引号）

**3. 把 Token 贴进 Authorize**
- ▶ 点页面右上角 `Authorize` → 输入框粘贴 token → `Authorize` → `Close`
- ✅ 锁图标变成"已上锁"状态

**4. 查自己的订单**
- ▶ 展开 `GET /api/orders/{id}` → `Try it out` → id 填 `1001` → `Execute`
- ✅ 响应码 `200`，返回订单内容

**5. 去掉 Token 再查一次**
- ▶ 再点 `Authorize` → `Logout`（清空）→ 重新 `Execute` 上面这个请求
- ✅ 响应码变成 `401`

> 到这里你已经亲眼见过 200、401 和 Token 长什么样。**这就是后面所有断言的原型。**

## A-2 让 AI 出场景表

**6. 复制下面这段提示词**
- ▶ 在 AI 工具里 `@` 引用 `docs/openapi.json` 和 `docs/requirements.md`，粘贴提示词

🤖 **提示词 1：生成场景表（先别写代码）**

```text
你是一名 API 测试工程师。根据 @docs/openapi.json 和
@docs/requirements.md，为 /api/orders 相关接口设计集成测试。

先不要写代码。对每个接口，按以下类别推导场景：
正常、缺少必填字段、取值非法、未认证、无权限、
资源不存在、业务冲突。
输出表格：
| 接口 | 场景 | 请求要点 | 期望状态码 | 响应体检查 | 副作用检查 |

确认后，用 xUnit + WebApplicationFactory 生成测试。约束：
- 用辅助方法 LoginAs(user) 现场获取 Token，不得硬编码
- 每个测试独立准备数据，不依赖执行顺序
- 金额期望值按 requirements.md 计算并注明过程
- 不要修改被测代码；生成后运行并报告
```

- ✅ AI 输出的表里，下面 7 类都能对上：正常、缺字段、取值非法、未认证、无权限、不存在、冲突

**7. 人工补漏（这一步必须做）**
- ▶ 逐行对照上面的 7 类，缺哪类就回一句："还缺 XX 场景，补上"
- ✅ 表里出现"数量 0 / -1 / 99 / 100"四个边界，并且**出现"别人的订单 / 403 或 404"这一行**
- ⚠ AI 十有八九会漏掉"越权"这一行，这不是你的错，是它的通病，必须你自己加

## A-3 生成并跑通 API 测试

**8. 让 AI 按场景表写代码**
- ▶ 回一句："按这张表生成测试代码，约束照旧"
- ✅ 生成的文件里有 `LoginAs` 这样的辅助方法，Token 是现场登录拿的，不是写死的长字符串

**9. 跑起来**
- ▶ `dotnet test --filter OrderApiTests`
- ✅ 多数通过；如果出现 `Expected: BadRequest Actual: Created`（数量 -1），**这是预埋缺陷 4，先记下来别修**

**10. 逐项审查（照着勾）**

| 勾 | 检查什么 |
| --- | --- |
| ☐ | 七类场景是否都在 |
| ☐ | 有没有"只断言 200"的测试 |
| ☐ | 代码里有没有一长串写死的 Token 或订单号 |
| ☐ | 期望状态码里有没有出现 500（出现就是错的） |
| ☐ | 有没有越权测试 |
| ☐ | 至少 1 条测试同时检查了"状态码 + 响应体 + 副作用"（三层断言） |

- ✅ 六项全勾上，缺的让 AI 补

## A-4 做越权测试，抓缺陷 7

**11. 先让 AI 补一条越权测试**

🤖 **提示词 2：补越权测试（讲义整理，PPT 未给完整提示词）**

```text
为 GET /api/orders/{id} 补一条越权测试：
- bob 先自己创建一笔订单，拿到订单号
- alice 用自己的 Token 去请求 bob 的这笔订单
- 期望 403 或 404
- 订单号必须由测试自己创建，不得写死
```

- ▶ 跑 `dotnet test --filter GetOrder_OtherUsersOrder`
- ✅ 测试**失败**，报 `Actual: OK`——这就是缺陷 7 被抓住的样子

**12. 手动复现一次（必须做，比看测试报告震撼）**
- ▶ 回 Swagger，用 alice 登录拿 token → `Authorize` → 请求 `GET /api/orders/1002`
- ✅ 返回 `200`，响应体里是 **bob** 的地址和手机号
- ▶ 截图，这张图要放进缺陷报告

**13. 写缺陷报告**
- ▶ 新建一个 Markdown 文件，按这 6 行填：

```text
标题：越权查看他人订单
严重程度：高
接口：GET /api/orders/{id}
复现步骤：alice 登录拿 Token → 请求 bob 的订单 1002
实际结果：200，返回 bob 的地址、电话
期望结果：403 或 404，且不返回订单内容
```

- ✅ 报告里有手动复现截图 + 失败测试的截图

## A-5 让 AI 修，然后验收

**14. 修越权**

🤖 **提示词 3：修复越权（讲义整理）**

```text
修复越权问题：查询订单时检查订单的 UserId 是否等于当前登录用户，
不匹配返回 404。不要修改测试，不要修改 .github 下的文件。
修复后运行全部 API 测试并报告结果。
```

- ▶ 再跑第 11 步那条测试
- ✅ 变成 `Passed`
- ⚠ 顺手问 AI 一句："还有哪些接口也接收订单 id？"——取消订单、订单详情往往被漏掉

**15.（选做）修缺陷 4 和 5**

🤖 **提示词 4：入口校验（讲义整理）**

```text
修复两个缺陷：
1. 下单接口接受负数数量：数量必须为 1..99 的整数，否则返回 400；
2. 登录接口缺少 username 时抛空引用异常：在入口校验，返回 400 和明确错误信息。
不要修改测试和 workflow。修复后运行全部测试并报告。
```

- ▶ `dotnet test`
- ✅ 全绿

**阶段 A 交付物**：场景表（含你补的越权那行）、API 测试代码与运行截图、缺陷 7 报告（含手动复现截图）。

---

# 阶段 B Playwright E2E

## B-1 装环境，跑通示例

**16. 切分支、开两个终端**
- ▶ `git switch week10-start`
- ▶ 终端 1：`dotnet run --project csharp/src/CampusShop.Web`
- ✅ 终端 1 出现 `Now listening on: http://localhost:5000`

**17. 构建 E2E 项目**
- ▶ 终端 2：`cd csharp/tests/CampusShop.E2E`，然后 `dotnet build`
- ✅ 显示 `Build succeeded`

**18. 装浏览器**
- ▶ `pwsh bin/Debug/net10.0/playwright.ps1 install chromium`
- ✅ 进度条走完，没有红色报错

**19. 跑示例测试**
- ▶ `dotnet test`
- ✅ `Passed! - Failed: 0, Passed: 1`
- ▶ 想看见浏览器：`$env:HEADED="1"; dotnet test`

## B-2 录一个登录测试

**20. 启动录制**
- ▶ `pwsh bin/Debug/net10.0/playwright.ps1 codegen http://localhost:5000`
- ✅ 弹出两个窗口：左边浏览器，右边代码窗口

**21. 在浏览器里走一遍**
- ▶ 输入 `alice / 123456` → 点登录
- ✅ 页面显示"欢迎，alice"

**22. 让录制帮你加断言**
- ▶ 代码窗口工具栏点"断言文字"，再点页面上的"欢迎，alice"
- ✅ 代码窗口里多出一行断言

**23. 清理生成的代码（三件事）**
- ▶ 删掉每个"填写"前面多余的"点击"那行
- ▶ 检查定位符，不是"按角色/按标签/按 testid"的，全部换掉
- ✅ 代码里没有 `sleep`、没有 `#app > div` 这种层级路径

**24. 存成测试文件并运行**
- ▶ 粘进测试文件 → 清掉环境变量 `Remove-Item Env:HEADED` → `dotnet test --filter Login`
- ✅ 绿

## B-3 让 AI 写"下单"测试（两步法）

**25. 第一步：让 AI 先看页面，不写代码**

🤖 **提示词 5：探索页面**

```text
第一步（不写代码）：
使用 Playwright 工具打开 http://localhost:5000，完成以下流程：
用 alice / 123456 登录 → 把“笔记本”加入购物车，数量 2
→ 进入购物车 → 点击下单。
记录每一步用到的元素（角色、名称或标签）
以及每一步之后页面上出现的关键信息。
```

- ✅ AI 回给你一张"步骤 → 元素"的清单，比如：输入框"用户名"、按钮"登录"、testid=cart-total、testid=order-no

**26. 第二步：按清单生成测试代码**

🤖 **提示词 6：生成下单测试**

```text
第二步：根据记录，用 NUnit + Playwright 写 E2E 测试
Order_TwoNotebooks_ShowsOrderNumber。要求：
- 定位符优先 GetByRole / GetByLabel，其次 GetByTestId
- 断言用 Expect(...)，不得使用 sleep / WaitForTimeout
- 断言购物车总价（按需求计算）与订单号格式
- 测试自己登录，不依赖其他测试
```

- ▶ 跑 `dotnet test --filter Order_TwoNotebooks`
- ✅ 绿

**26-B. 同一轮对话里，再要一个"登录失败"测试**

🤖 **提示词 6-B：登录失败（讲义整理）**

```text
再写一个 E2E 测试 Login_WrongPassword_ShowsError：
用 alice 和错误密码登录，断言页面上出现“用户名或密码错误”。
错误提示用 GetByTestId("error-msg") 定位，不要 sleep，
不要只断言网址还停在登录页。
```

- ▶ `dotnet test --filter Login_WrongPassword`
- ✅ 绿，而且断言的是**错误提示文字**，不是网址
- ⚠ 只断言"还停在 /login"等于没测——那个页面本来就不会跳走

**27. 两条断言必须亲眼看到**
- ▶ 打开生成的代码，找这两行
- ✅ 一条断言总价是 `¥90.00`（50 × 2 = 100，满 100 减 10）；一条断言订单号格式以 `CS` 开头
- ⚠ 只有"页面标题对""网址对"的测试等于没测，退回去让 AI 补

## B-4 调试三招（红的时候用）

| 招式 | 怎么用 | 什么时候用 |
| --- | --- | --- |
| 有头模式 | `$env:HEADED="1"; dotnet test` | 想亲眼看浏览器卡在哪一步 |
| Inspector | `$env:PWDEBUG="1"; dotnet test --filter 测试名` | 元素找不到，想现场试定位符 |
| 断点 | VS Code 在 `await` 那行打断点 | 想看变量值 |

- ▶ 用完记得清：`Remove-Item Env:HEADED, Env:PWDEBUG`
- ✅ `dotnet test` 恢复正常速度

## B-5 抓缺陷 8：空购物车还能下单

**28. 让 AI 写这个测试**

🤖 **提示词 7：空购物车（讲义整理）**

```text
写一个 E2E 测试：用 bob 登录（购物车为空）→ 进入购物车 →
断言页面显示“购物车为空”，并且“下单”按钮处于禁用状态。
定位符用 GetByRole / GetByText，断言用 Expect，不要 sleep。
```

- ▶ `dotnet test --filter Cart_Empty`
- ✅ 测试**失败**，报"期望禁用，实际可用"——这就是缺陷 8

**29. 有头模式看一眼后果**
- ▶ `$env:HEADED="1"; dotnet test --filter Cart_Empty`
- ✅ 亲眼看到按钮可以点，并记下点完之后发生了什么（报错还是生成了 0 元订单）
- ▶ 截图，写缺陷报告，严重程度按后果定

**30. 交审查记录**
- ▶ 一页纸，写清"发现了哪几处问题 → 改成了什么"
- ✅ 至少 1 条，例如："AI 用了 `WaitForTimeout`，改成了 `Expect(...)`"

**阶段 B 交付物**：3 个 E2E 测试（登录成功、登录失败、下单成功）+ 运行截图 + 审查记录。

---

# 阶段 C CI 流水线

## C-1 把代码推到自己的仓库

**31. 确认登录、改远程**
- ▶ `gh auth status`
- ✅ `Logged in to github.com as 你的用户名`
- ▶ `git remote rename origin upstream`
- ✅ `git remote -v` 里能看到 `upstream`

**32. 建自己的仓库并推送**
- ▶ `gh repo create campusshop-你的名字 --public --source . --remote origin --push`
- ✅ 终端显示 `Created repository` 和 `Pushed commits`
- ▶ `gh repo view --web`
- ✅ 浏览器里能看到你的代码，顶部有 `Actions` 标签

## C-2 让 AI 写流水线

**33. 复制提示词**

🤖 **提示词 8：生成 workflow**

```text
为 CampusShop 的 C# 版本编写 GitHub Actions
workflow（.github/workflows/ci.yml）：
- 触发：push 到 main、所有 pull_request
- 作业 1：运行单元测试与 API 测试，收集覆盖率，
  始终上传测试结果
- 作业 2：在作业 1 通过后运行 E2E 测试：后台启动
  网站、等待就绪、安装 chromium、运行测试；
  失败时上传 traces 目录
- 只使用 actions/ 组织下的官方动作
- 不得忽略任何测试失败
```

- ✅ 生成了 `.github/workflows/ci.yml`

**34. 逐项审查（这一步不能省）**

| 勾 | 搜什么 | 看到就该做什么 |
| --- | --- | --- |
| ☐ | `continue-on-error` | 删掉这一行 |
| ☐ | `\|\| true` | 删掉，命令失败必须让步骤失败 |
| ☐ | `--filter` | 问清楚为什么只跑一部分，多半要删 |
| ☐ | `pull_request` | 必须有，否则 PR 不过 CI |
| ☐ | `secrets` | 本课不需要任何密钥，有就问 AI 为什么 |
| ☐ | `dotnet-version` | 要和本地一致（10.0.x） |

- ✅ 六项全部确认过，才允许提交

## C-3 推上去，看它变绿

**35. 提交并观察**
- ▶ `git add .github`，`git commit -m "添加 CI 流水线"`，`git push`
- ▶ `gh run watch`
- ✅ 两个作业依次变绿：`unit-and-api`、`e2e`
- ⚠ 第一次红了很正常，跳到 C-4，别在网页上瞎点

## C-4 红了怎么修（固定流程，别在 CI 上试错）

**36. 读失败日志**
- ▶ `gh run list --limit 3` 找到失败的那次编号
- ▶ `gh run view 编号 --log-failed`
- ✅ 看到具体是哪个测试、期望什么、实际什么

**37. 本地复现**
- ▶ 用同样的命令在本地跑同一个测试
- ✅ 本地也红 → 是代码/测试问题；本地怎么都不红 → 是环境差异，跳到 **C-5**

**38. 让 AI 修（带约束）**

🤖 **提示词 9：修 CI 失败**

```text
CI 的 unit-and-api 作业失败。日志如下：
<把 gh run view --log-failed 的输出粘在这里>

诊断：<写你判断的原因，例如：代码用了本地时间，CI 是 UTC>

请修复这个问题。约束：
- 不得修改 .github/ 下的任何文件
- 不得修改测试的期望值
- 不得使用 Skip 或注释掉测试
完成后在本地运行全部测试并报告结果。
```

- ▶ 本地 `dotnet test` 全绿后再 `git push`
- ✅ `gh run watch` 看到变绿

> ⚠ **最容易踩的坑**：只说"帮我修好 CI"，AI 可能给你加 `continue-on-error: true`，灯绿了但测试根本没跑。所以提示词里那三条约束一条都不能删。

## C-5 本地绿、CI 红？对照这张表

| 现象 | 去改哪里 |
| --- | --- |
| 文件明明有却报找不到 | 文件名大小写：Linux 区分，改成和实际完全一致 |
| `Permission denied` 跑 `mvnw` | 加一步 `chmod +x java/mvnw`，或本地 `git update-index --chmod=+x java/mvnw` |
| `bad interpreter: /bin/bash^M` | 换行符是 CRLF，在 `.gitattributes` 里设 `*.sh text eol=lf` |
| 和白天的结果不一样 | 时区：CI 是 UTC，代码别用"当前时间"，改成注入时钟 |
| 数字/日期格式对不上 | 区域设置：比较数值，不要比较格式化后的字符串 |
| 路径拼错 | 用 `Path.Combine` / `Paths.get`，别手拼 `\` |

## C-6 开分支保护，让红灯真的拦人

**39. 设置规则**
- ▶ 仓库 `Settings` → `Branches`（或 `Rules`）→ 为 `main` 添加规则
- ▶ 勾选：要求通过 PR 合并
- ▶ 勾选：要求状态检查通过 → 选中 `unit-and-api` 和 `e2e`
- ▶ 可选：要求 1 人批准；不允许管理员绕过
- ✅ 规则保存成功

**40. 验证它真的生效**
- ▶ `git switch main`，`git commit --allow-empty -m "直推试试"`，`git push`
- ✅ 被拒绝，提示 `Changes must be made through a pull request`
- ▶ 走 PR：`gh pr create --fill` → `gh pr checks` → `gh pr merge --squash`
- ✅ CI 绿时合并按钮可点；红时是灰的

**阶段 C 交付物**：仓库链接、CI 运行截图、PR 链接、workflow 审查记录（写明有没有发现坏把戏）。

---

# 卡住了看这里

| 你看到的 | 八成是这个原因 | 怎么办 |
| --- | --- | --- |
| `ERR_CONNECTION_REFUSED` | 网站没启动 | 回阶段 0 第 2 步，终端 1 必须是活的 |
| 一直 `401` | Token 少了 `Bearer ` 前缀 | 重新 Authorize，或检查请求头 |
| `pwsh 不是内部或外部命令` | 没装 PowerShell 7 | `winget install Microsoft.PowerShell`，重开终端 |
| `Executable doesn't exist ... chromium` | 浏览器没装 | 重跑 `playwright.ps1 install chromium` |
| 元素找不到 | 定位符是猜的 | `PWDEBUG=1`，在 Inspector 里试 |
| 匹配到多个元素 | 定位符太宽 | 限定到某一行/某个容器内 |
| 测试单独跑能过、一起跑就挂 | 共用了一个用户的购物车 | 每个测试换独立用户，或先清空 |
| `gh: command not found` | 没装 GitHub CLI | `winget install GitHub.cli` |
| CI 报 `Permission denied` | `mvnw` 没有执行权限 | 见 C-5 第二行 |
| PR 页面没有检查项 | 触发条件少了 `pull_request` | 回 C-2 第 34 步补上 |

---

# 最后核对：交付清单

**阶段 A**
- ☐ 场景表（含你补的"越权"那行）
- ☐ 至少 6 个 API 测试，其中 3 个异常场景，1 个三层断言
- ☐ 缺陷 7 报告（含手动复现截图）

**阶段 B**
- ☐ 3 个 E2E 测试：登录成功、登录失败、下单成功
- ☐ 运行截图
- ☐ 审查记录（至少 1 处修正）

**阶段 C**
- ☐ CI 截图（两个作业都绿）
- ☐ PR 链接 + CI 检查结果
- ☐ 分支保护生效的截图
- ☐ workflow 审查记录（含"是否发现坏把戏"）

> 全部勾完，阶段作业 2 的素材就齐了。
> 需要看每一步"为什么这么做"、代码怎么写、坑在哪，回完整讲义《API 测试 · Playwright 端到端测试 · CI 流水线》。
