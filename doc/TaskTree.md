# TaskTree/Btree(任务树/行为树)

对于行为树，我有非常多想写的内容，但限于篇幅，这里只对核心部分进行说明。  
在开始以前，我要强调一下，我这里的提供的其实是通用的任务树，而不是局限于游戏AI的结构，你可以用它做你一切想要的逻辑，技能脚本、副本脚本、任务脚本。。。

## Task(任务)

Task代表一个异步任务，不过与一般异步任务不同的是：Task不会自动运行，需要用户每帧调用`TaskEntry`的`update`方法来驱动任务的执行。

可简单表示为：

```
   int frame = 0;
   while (true) { // 这个死循环指线程的死循环，在游戏开发中称为主循环
      frame++;
      taskEntry.update(frame);
   }
```

## Context(上下文)

Task的上下文由三部分组成：黑板、取消令牌、共享属性。  
每个Task都可以有独立的上下文，当未指定Task的上下文时，会默认从父节点中捕获缺失的上下文。

```
   class Task {
      Object blackboard;
      CancelToken cancelToken;
      Object sharedProps;
   }
```

### Blackboard(黑板)

有过行为树编程经验的，一定对黑板很熟悉。简单说：**黑板就是一块共享内存**，Task通过黑板共享信息，Task既从黑板中读取(部分)
输入，也将(部分)输出写入黑板。

在我们的实现中，每个Task都可以有独立的黑板，未显示绑定的情况下，默认从父节点继承；Task并未限定黑板的类型，黑板完全由用户自身实现。

#### 黑板实现的指导

1. 黑板通常需要实现父子关系，在当前黑板查询失败时自动从父黑板中查询 -- (local + share 或 local + parent)。
2. 不要在黑板中提供过多的功能，尽量只保持简单的数据读写，就好像我们使用内存一样。
3. 如果要实现数据更新广播，可参考volatile字段的设计，即只有特定标记的字段更新才派发事件 -- 避免垃圾事件风暴。

### CancelToken(取消令牌)

由于Task是复杂的树形结构，而取消要作用于一组任务，而不是单个任务，因此这些任务必须共享一个上下文来实现监听；
我考虑过将取消信号放入黑板，但这可能导致糟糕的API，或限制黑板的扩展；而通过额外的字段来共享对象将大幅简化设计，而没有明显的缺陷。

#### 协作式取消

任何的异步任务，不论单线程还是多线程，立即取消通常都是不安全的，因为取消命令的发起者通常不清楚任务当前执行的代码位置；
安全的取消通常需要任务自身检测取消信号来实现，只有任务自身知道如何停止。

在默认情况下，Task只会在心跳模板方法中检测取消信号，而不会向CancelToken注册监听器；不过我提供了控制位允许用户启用自动监听，
通常而言，我们的大多数业务只在TaskEntry启用自动监听接口，因为通常是取消整个任务。

ps：jdk的Future的取消接口，在异步编程如火如荼的今天，已经不太适合了，我后面会写一套自己的Future。

### SharedProps(共享属性/配置)

共享属性用于解决**数据和行为分离**
架构下的配置需求，主要解决策划的配置问题，减少维护工作量。我以技能配置为例，进行简单说明。  
在许多项目中，角色技能是有等级的，或是按公式计算，或是每一级单独配置，但不论选择那种方案，技能数值最好都抽离出来；
就好像脚本一样，技能的数值全部在脚本的开头定义，而逻辑则放在后面，这样策划在调整数值的时候就比较方便。  
当我们使用复杂的树形结构来做技能脚本时，就需要每个Task都需要能读取到这部分属性，共享属性就是为这样的目的服务的。

ps：你可以将共享属性理解为另一个黑板，只是我们只读不写。

## 心跳+事件驱动

行为树虽然提供了事件驱动支持，但**心跳驱动为主，事件驱动为辅**。  
我一直反对纯粹的事件驱动，有几个重要的理由：

1. 子节点将自己加入某个调度队列，导致父节点对子节点丧失控制权，也丧失时序保证 -- Unity开发者尤为严重。
2. 事件驱动会大幅增加代码的复杂度，**因为事件是跳跃性思维，而心跳是顺序思维**。
3. 事件也是一种信号，错失信号可能导致程序陷入错误无法恢复 -- 你玩游戏碰见的很多bug都源于此。
4. 不要老拿性能说问题，首先大多数的行为树不深；另外，**脱离你掌控的代码一定不是好代码**。

事件驱动分两部分：child的状态更新事件 和 外部事件。  
child状态更新事件，主要是子节点进入完成状态的事件，父节点通常在处理该事件中计算是否结束；
外部事件是指外部通过`onEvent`派发给TaskEntry的事件，叶子节点一般直接处理事件，非并行节点一般转发给运行中的子节点，
并行节点一般派发给主节点（第一个子节点）；如果有特殊的需求，则需要用户自己扩展。

## reentry(重入)

重入的概念：重入是指Task上一次的执行还未完全退出，就又被父节点再次启动。  
以状态机为例（状态机中最常见），假设现在有一个状态A，在`execute`时检测到条件满足，请求状态机再次切换为自己；
由于上一次的执行尚未完全退出，因此现在**有"两个"状态A都在execute代码块**，我们称这种情况为重入。

重入的危险性：调用状态机的`changeState`会触发当前状态的`exit`方法，然后触发新状态的`enter`方法，对于前一个状态A的执行而言，task的上下文已彻底变更；
如果前一个状态A的执行没有立即return，就可能访问到错误的数据，从而造成错误 --
*在以往的工作中便出现过忘记return导致NPE的情况*。

ps：想到一个经常遇见的问题，List在迭代的时候删除元素。

```
   public void enter() {
      // 初始化一些数据
   }

   public void execute() {
      if (cond()) {
         stateMachine.changeState(this); // 自己切换自己的情况不常见（但存在），更多的情况是不知不觉中绕一圈。
         return; // 这里如果没有return是有风险的
      }
   }
   public void exit() {
      // 清理一些数据
   }
```

在之前的项目中，我在这个问题上犯了大错误。由于我没想到好的方案来解决这个问题，于是我便*阻止用户编写这样的代码*，
我添加机制检测任务是否正在执行，如果正在执行则抛出错误。
实现这套重入检测机制，大幅增加了代码的复杂度，然而由于开销等问题，又不能做非常精确的检测，只能检测到一般的重入错误。

这套机制在之前的项目没有造成太大影响，因为我在使用状态机这类节点的时候，都不复用对象，而是创建新对象，这样就避免了这个问题。
但这始终是一个缺陷，因为我们不能立即复用对象，行为树也就不能被立即reset然后重用，而是要延迟reset再归还到池，这导致了业务代码变得复杂。

### 重入检测

在说解决方案前，要先说我到底犯了什么错。可以这样表述：  
**代码是逻辑，是不确定的东西，你永远无法判断用户逻辑的正确性，代码应该怎样运行，应当由用户说了算。**
你不能因为担心出错，而禁止用户的合法需求；另外，你认为可能是错误的东西，可能是在用户的掌控中，而是正确的。
因此，要么你什么也不做，要么就帮助用户检测错误。如果你的系统在某些情况下会出错，而其它情况下不会出错，那这个系统就不是一个可靠的系统。

**解决方案**：  
我们在每个Task上记录一个`reentryId`，当Task的生命周期发生变更时+1；用户在执行不确定代码前，先将reentryId保存下来，执行不确定代码后，
通过检查重入id的相等性就可得知Task的生命周期是否发生了变更，以及是否已经被重入。

ActionTask的模板方法如下：

```
   public void execute() {
        int reentryId = getReentryId();
        int status = executeImpl();
        if (isExited(reentryId)) { // 当前任务已退出
            return;
        }
        // ... 更新状态
   }
```

## 状态机

在讲行为树的时候提状态机是不是有点怪怪的？有些程序可能受游戏AI开发的影响，认为行为树和状态机是互斥的，也认为状态机是个过时的东西，
毕竟网上的文章大多是这种： "有限状态机时代终结的10大理由"，"从有限状态机到行为树"。。。

这里我要给大家纠正一下，这类文章是有害的，大多数是跟风者的言论。以我的经验告诉你：

1. **有限状态机(FSM)永远是你的最重要的工具之一**，它可以解决绝大多数的问题。
2. **事件驱动的行为树与状态机并不互斥**，可完美的结合 -- 我这里的状态机就是一个Task类型。

在之前的项目中，我就已经大规模的使用TaskTree，根本不需要额外的工具；如今的实现更优，几乎没有限制，用来做副本脚本、技能都是很容易的。  
ps：状态机有独立的测试用例(`StateMachineTest`)，大家可以跑一跑。

### 细微区别

在传统的状态机下，在切换状态时只会调用新状态的`enter`方法，下一帧才会调用`execute`方法；但在这里，`execute`方法和`enter`
通常是连续执行的，这在多数情况下是没有影响的；如果确实需要分开执行，我们提供了控制位标记，以允许你将自己的状态标记为需要分开执行。

```
   // 传统状态机的状态切换代码
   public void changeState(State nextState) {
      if(curState != null) {
         curState.exit();
      }
      curState = nextState;
      if (curState != null) {
         curState.enter();
      }
   }
```

## 题外话

我首次了解到行为树，大概是工作的第二年，当时项目的游戏对象AI是通过状态机实现的，我个人对游戏的AI和技能实现是非常感兴趣的，
所以在熟悉一般业务后，一直在研究（折腾）项目的AI和战斗系统，当时有同事给我说也有AI是行为树实现的，于是我就去查了一下资料，没有搞太懂，就先放一边了。
在回到成都后，关卡策划想使用使用行为树配置对象AI，我心里其实很虚，因为我压根儿没写过；但我也想尝试一下，毕竟我也想做好玩的游戏。

这一研究就不得了，在项目开发期间几乎没停止，这期间我实现了好几个版本，大概在1年左右的时候才得到一个还算满意的版本。花了这么多精力，有两个重要的原因：

1. 在我理解行为树的概念后，我发现**行为树其实不是仅限于AI的，而是一套很好的控制流结构**，是可以通用的。我想让其用于
   **技能、副本玩法、任务**的配置。而要达到这一点，必须要支持事件驱动。
2. 行为树的事件驱动和事件处理非常不好写，我被两个问题卡住了：
    1. 事件驱动的方法栈非常深
    2. 事件驱动可能导致Task递归（重入），如何保证上下文的正确性

我能力上有一个缺陷：缺乏想象力，没见过的东西，我就造不出来，或很难造出来！  
写行为树的时候也是如此，所以我在网上大量查找他人的行为树的实现，对我有较大影响的是[GDX_AI](https://github.com/libgdx/gdx-ai)；
相比于其它行为树的实现，GDX_AI的行为树实现还算能看，但研究之后觉得它的行为树不能用，有以下几点：

1. 有奇怪的看不懂的代码，例如：先调用`child.start`，然后再`child.checkGuard(this)`，这不符合我期望。
2. 方法栈深度爆炸，没有任何优化
3. 没有解决Task重入的安全性问题
4. 没有像状态机那样的事件支持

我在上个项目中实现的行为树，没有彻底解决的就是重入安全性问题，因此只能说能用，但有限制。最近我在考虑这个开源项目下一步写什么的时候，想到了行为树，
因为行为树的应用可以很广，可以作为一个非常基础的组件。在大半年没碰行为树的情况下，这次实现行为树，居然出乎意料的顺利（1周就搞定），而且曾经想到的问题几乎都解决了。  
这里有几点心得分享：

1. 首次使用一门技术的人容易发现新事物，**要注意新人对框架的意见**。
2. 苦思冥想通常没有结果，放一放，有时候就会有了新思路。
3. 如果只有用户才能保证正确性，那就不要在框架层做操作限制，而是配合用户检测冲突，**半成品的东西通常比什么都不做还糟糕**。