# Ant 风格路径匹配规则

## 一、通配符

| 通配符 | 语义                                          |
|--------|-----------------------------------------------|
| `?`    | 匹配**恰好一个**字符，不能跨越路径分隔符       |
| `*`    | 匹配**零个或多个**字符，不能跨越路径分隔符     |
| `**`   | 匹配**零个或多个**路径段（目录层级），可跨越分隔符 |

> 默认路径分隔符为 `/`。`**` 必须作为完整路径段出现（即 `/**/` 或位于末尾），出现在路径段内部时（如 `a**b`）行为等价于 `*`，具体取决于实现。

---

## 二、`?` — 单字符通配

恰好匹配一个字符，不能匹配路径分隔符。

| 模式              | 路径               | 匹配 |
|-------------------|--------------------|------|
| `com/t?st.jsp`    | `com/test.jsp`     | ✓    |
| `com/t?st.jsp`    | `com/tast.jsp`     | ✓    |
| `/index?`         | `/index`           | ✗（缺少一个字符）  |
| `/index?`         | `/indexab`         | ✗（超过一个字符）  |
| `/index?`         | `/index/`          | ✗（不匹配分隔符）  |

---

## 三、`*` — 单段多字符通配

匹配零个或多个字符，作用范围限于**单个路径段**（不跨越 `/`）。

| 模式          | 路径                 | 匹配 |
|---------------|----------------------|------|
| `com/*.jsp`   | `com/test.jsp`       | ✓    |
| `com/*.jsp`   | `com/.jsp`           | ✓（零个字符）  |
| `com/*.jsp`   | `com/foo/bar.jsp`    | ✗（跨越了目录）|
| `*.java`      | `Test.java`          | ✓    |
| `*.java`      | `test/Test.java`     | ✗    |

---

## 四、`**` — 多段通配

匹配零个或多个路径段，可跨越 `/`。

| 模式                        | 路径                               | 匹配 |
|-----------------------------|------------------------------------|------|
| `com/**/test.jsp`           | `com/test.jsp`                     | ✓（零级）  |
| `com/**/test.jsp`           | `com/foo/test.jsp`                 | ✓    |
| `com/**/test.jsp`           | `com/foo/bar/test.jsp`             | ✓    |
| `org/**/servlet/bla.jsp`    | `org/servlet/bla.jsp`              | ✓（零级）  |
| `org/**/servlet/bla.jsp`    | `org/springframework/servlet/bla.jsp` | ✓ |
| `org/**/*.jsp`              | `org/springframework/web/index.jsp` | ✓   |
| `/test/**`                  | `/test/foo/bar/xyz.html`           | ✓    |

### 尾部斜线快捷规则

模式以 `/` 结尾时，等价于自动附加 `**`：

```
mypackage/test/  =>  mypackage/test/**
```

---

## 五、URI 模板变量

### 基本捕获

格式：`{variableName}`，匹配单个路径段内的任意字符（不跨 `/`）。

| 模式                   | 路径                    | 捕获结果              |
|------------------------|-------------------------|-----------------------|
| `/hotels/{hotel}`      | `/hotels/1`             | `hotel=1`             |
| `/users/{id}/posts/{postId}` | `/users/123/posts/456` | `id=123, postId=456` |

### 带正则约束

格式：`{variableName:regex}`，变量值必须完整匹配正则表达式。

| 模式                        | 路径              | 匹配 | 捕获结果         |
|-----------------------------|-------------------|------|------------------|
| `/users/{name:[a-z]+}`      | `/users/john`     | ✓    | `name=john`      |
| `/users/{name:[a-z]+}`      | `/users/John123`  | ✗    | —                |
| `/files/{file:\\w+\\.\\w+}` | `/files/doc.pdf`  | ✓    | `file=doc.pdf`   |

---

## 六、匹配优先级

同一路径被多个模式匹配时，优先级从高到低：

```
1. 精确匹配（无任何通配符和变量）   /hotels/new
2. URI 模板变量（无通配符）         /hotels/{hotel}
3. 通配符 ? 或 *                    /hotels/*
4. 双星号 **                        /hotels/**
5. 最泛化模式                        /**
```

**决策依据**（同级时再比较）：
- 通配符越少，优先级越高
- URI 变量越少，优先级越高
- 模式越长（更具体），优先级越高

**示例**：请求路径 `/hotels/new`，多个模式排序后：

```
/hotels/new      ← 精确，最高
/hotels/{hotel}  ← 变量
/hotels/*        ← 单级通配
/hotels/**       ← 多级通配
/**              ← 最低
```

---

## 七、路径分隔符与绝对/相对路径

- 模式与路径必须**同为绝对路径**或**同为相对路径**：
  - `/test/*` vs `/test/foo` → ✓
  - `test/*` vs `test/foo` → ✓  
  - `/test/*` vs `test/foo` → ✗（混用）
- 路径分隔符可配置，默认为 `/`。

---

## 八、大小写与空白

- **默认区分大小写**：`/Hotels/*` 不匹配 `/hotels/index`。
- 不建议对路径段做空白修剪；尾部空格会导致匹配失败。

---

## 九、边界情况汇总

| 场景              | 模式         | 路径    | 匹配 |
|-------------------|--------------|---------|------|
| 空字符串对空字符串 | ``           | ``      | ✓    |
| `*` 匹配空段       | `/*`         | `/`     | ✓    |
| `**` 匹配空路径    | `/**`        | `/`     | ✓    |
| 变量不匹配空段     | `/{foo}`     | `/`     | ✗    |
| `?` 不匹配分隔符   | `/a?b`       | `/a/b`  | ✗    |
| `**` 匹配零级      | `/a/**/b`    | `/a/b`  | ✓    |

---

## 十、与 Glob 的主要区别

| 特性            | Ant 模式                   | Glob（Java NIO 等）         |
|-----------------|----------------------------|-----------------------------|
| `*` 跨越分隔符  | ✗ 严格限制在单段内          | 视实现而定，通常可跨越      |
| `**` 语义       | 必须作为完整路径段，匹配多级 | 较灵活但实现各异            |
| URI 变量捕获    | 支持 `{var}` 和 `{var:re}` | 不支持                      |
| 面向场景        | URL / 资源路径              | 文件系统路径                |

---

## 十一、常用模式示例

```
/api/**                  匹配 /api 下所有路径
/api/users/{id}          匹配 /api/users/123
/api/*/detail            匹配 /api/foo/detail，不匹配 /api/foo/bar/detail
/static/**/*.{js,css}    匹配 /static/app/main.js
com/example/**/*Test.class  匹配所有测试类
/v{version:[0-9]+}/**    匹配 /v1/... /v2/...
```

---

## 参考

- [Apache Ant Manual — Directory-based Tasks](https://ant.apache.org/manual/dirtasks.html)
- [Spring Framework — AntPathMatcher](https://docs.spring.io/spring-framework/docs/current/javadoc-api/org/springframework/util/AntPathMatcher.html)
- [Spring Framework — Path Matching](https://docs.spring.io/spring-framework/reference/web/webmvc/mvc-servlet/handlermapping-path.html)
