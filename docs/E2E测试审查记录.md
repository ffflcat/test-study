# E2E 测试审查记录

- 审查日期：2026-11-02
- 审查范围：3 条 Web 端 E2E 用例（登录成功、登录失败、下单成功）
- 被测对象：CampusShop 校园商城（`csharp/src/CampusShop.Web`），运行于 `http://localhost:5000`
- 技术栈：.NET 10.0、NUnit 4.3.2、NUnit3TestAdapter 5.0.0、Microsoft.Playwright 1.64.1、Microsoft.Playwright.NUnit 1.64.1、Microsoft.NET.Test.Sdk 17.14.1
- 测试工程：`csharp/tests/CampusShop.E2E`

## 一、测试清单

| 用例 | 场景 | 关键定位符 | 结果 |
|---|---|---|---|
| `Login_Alice_Success` | alice / 123456 登录成功 | `GetByLabel("用户名")`、`GetByLabel("密码")`、`GetByRole(Button, "登录")` | 通过 |
| `Login_WrongPassword_ShowsError` | alice / 错误密码登录失败 | `GetByTestId("error-msg")`、`GetByText("欢迎，alice")` | 通过 |
| `Order_TwoNotebooks_ShowsOrderNumber` | alice 登录 → 笔记本 × 2 加购 → 下单 | `GetByRole(Row)` 行内 `Spinbutton`/`Button`、`GetByTestId("cart-total")`、`GetByTestId("order-no")` | 通过 |

用例共同约束：

- 每个用例继承 `Microsoft.Playwright.NUnit.PageTest`，获得全新的浏览器上下文，用例自己登录，不依赖执行顺序或其他用例的 Cookie。
- 断言统一使用 `Expect(...)`（内置自动重试），**没有** `sleep` / `WaitForTimeout`。
- 定位优先级：`GetByRole` / `GetByLabel` → `GetByText` → 仅在前两者无法区分时使用 `GetByTestId`。

## 二、发现的问题与修改措施

3 条用例首跑全部通过，未发现被测应用的功能缺陷；但测试工程与测试写法存在下列 6 处问题，已全部修正。

### 问题 1：测试工程是 xUnit 模板，与 NUnit 要求不符

- **现象/原因**：`CampusShop.E2E.csproj` 引用 `xunit`、`xunit.runner.visualstudio`、`Microsoft.Playwright.Xunit`，模板残留 `UnitTest1.cs` 使用 `[Fact]` 与 `IClassFixture`；项目要求使用 NUnit。
- **修改**：包引用替换为 `NUnit` + `NUnit3TestAdapter` + `Microsoft.Playwright.NUnit`；删除 `UnitTest1.cs`；用例改为 NUnit 的 `[Test]`。
- **验证**：`dotnet build` 通过，NUnit Adapter 能发现并用例正常执行。

### 问题 2：所有用例共享一个 Page，且强制有头模式

- **现象/原因**：原 `PlaywrightFixture` 用 `IClassFixture` 让所有用例共用一个 `IPage`，登录 Cookie、页面位置会相互污染；同时 `Headless = false`，在无桌面/CI 环境无法运行。
- **修改**：改用 `PageTest` 基类。它为每个 `[Test]` 单独创建 `BrowserContext` 与 `Page`（默认无头），用例结束自动销毁；登录动作放进用例本身。
- **验证**：单跑与整包运行结果一致，说明无顺序依赖。

### 问题 3：商品表格中 6 组同名控件，全局定位会点错行

- **现象/原因**：商品列表 6 行都有 `spinbutton "数量"` 和 `button "加入购物车"`，直接取第一个会永远命中第一行，新增商品或调整顺序后用例即失效。
- **修改**：先用 `GetByRole(Row).Filter(Has: GetByRole(Cell, "笔记本", Exact))` 锁定行，再在该行内定位数量框与按钮，并先断言匹配行数 = 1。
- **验证**：用例稳定命中「笔记本」行，加购提示为“已将 2 件笔记本加入购物车”。

### 问题 4：订单号硬编码必然失败

- **现象/原因**：手工操作得到的订单号是 `CS5002`，但订单号由进程内自增计数器生成，重启或重复运行都会变化，写死具体号码会让用例不可重复执行。
- **修改**：用 `Expect(orderNumber).ToHaveTextAsync(new Regex(@"^CS\d+$"))` 只断言「`CS` + 数字」的格式，不绑定具体号码。
- **验证**：用例重复运行（多次下单）均通过。

### 问题 5：金额断言不能照抄页面数值

- **现象/原因**：直接在断言里写 `¥90.00` 无法说明金额来源，需求调整（如优惠门槛变化）时也无从判断对错。
- **修改**：用常量按需求推导，再格式化断言：
  - 原价 = 50.00 × 2 = 100.00
  - 需求 2.1「满 100 减 10」→ 应付 = 100.00 − 10.00 = 90.00
  - 同时断言 `原价合计：¥100.00` 与 `cart-total` = `¥90.00`
- **验证**：金额断言通过，推导过程保留在测试代码中。

### 问题 6：登录失败只断言网址，证明力不足

- **现象/原因**：登录失败时页面仍停留在登录页，只断言 URL 无法区分「提示了错误」与「点击没生效」。
- **修改**：断言 `GetByTestId("error-msg")` 可见且文本为「用户名或密码错误」，并反向断言「欢迎，alice」与「购物车」导航均不存在，确认未登录成功。
- **验证**：错误密码用例通过，错误提示与未登录状态同时被验证。

## 三、被测应用缺陷

上述 3 条用例首跑即通过，**未发现应用功能缺陷**。

同一批次补充的第 4 条用例（`Cart_Empty_ShowsEmptyStateAndDisabledCheckout`，bob 登录 + 空购物车）发现的 1 处缺陷记录在案：

| 项目 | 内容 |
|---|---|
| 需求依据 | `docs/requirements.md` 第 4.10 条：购物车为空时显示“购物车为空”，“下单”按钮不可点击 |
| 实际表现 | 空购物车页面按钮为 `enabled`；E2E 断言 `ToBeDisabledAsync` 失败，报错 `Locator expected to be disabled` |
| 原因 | `Pages/Cart.cshtml` 无条件渲染 `<button type="submit">下单</button>` |
| 修复 | 改为 `<button type="submit" disabled="@Model.Cart.IsEmpty">下单</button>`（Razor 在 `IsEmpty` 为 true 时输出 `disabled`，否则省略） |
| 服务端现状 | `CheckoutService.Checkout` 已对空购物车返回错误「购物车为空，无法下单」，本次只修视图，未改业务逻辑 |
| 验证 | 该用例复跑通过；下单用例（购物车非空）仍可正常点击，无回归 |

## 四、运行结果

修复后 E2E 工程整体运行：

```text
已通过 Cart_Empty_ShowsEmptyStateAndDisabledCheckout [1 s]
已通过 Login_Alice_Success [234 ms]
已通过 Login_WrongPassword_ShowsError [199 ms]
已通过 Order_TwoNotebooks_ShowsOrderNumber [461 ms]
测试总数: 4  通过数: 4
```

运行前置条件：`dotnet run` 在 `http://localhost:5000` 启动服务。

运行命令：

```bash
cd csharp/tests/CampusShop.E2E
dotnet test --filter "FullyQualifiedName~<用例名>"
# 或运行全部
dotnet test
```

## 五、遗留风险与建议

1. **种子数据是进程内状态**：每次下单扣减 2 件笔记本，连续运行约 50 次后库存耗尽，用例会因库存不足失败。长期回归前应重启服务或调用开发环境的 `POST /api/test/reset` 重置数据。
2. **固定端口**：用例基地址写死 `http://localhost:5000`，CI 中如换端口需同步调整；建议后续改为读取环境变量。
3. **订单号只验格式**：未校验订单号唯一性/连续性，如需覆盖应由 API 层测试承担。
4. **未覆盖**：登录空字段校验、购物车数量边界（1/99/100）、库存不足、支付失败等场景目前由 API/单元测试覆盖，若需要端到端保障可另行补充。
