/**
 * 可以被个人所有开源项目依赖的的基础模块
 *
 * @author wjybxx
 * date - 2023/12/24
 */
module wjybxx.base {
    requires jsr305;

    exports cn.wjybxx.base;
    exports cn.wjybxx.base.annotation;
    exports cn.wjybxx.base.collection;
    exports cn.wjybxx.base.ex;
    exports cn.wjybxx.base.io;
    exports cn.wjybxx.base.mutable;
    exports cn.wjybxx.base.pool;
    exports cn.wjybxx.base.reflect;
    exports cn.wjybxx.base.time;
    exports cn.wjybxx.base.tuple;

    opens cn.wjybxx.base;
    opens cn.wjybxx.base.annotation;
    opens cn.wjybxx.base.collection;
    opens cn.wjybxx.base.ex;
    opens cn.wjybxx.base.io;
    opens cn.wjybxx.base.mutable;
    opens cn.wjybxx.base.pool;
    opens cn.wjybxx.base.reflect;
    opens cn.wjybxx.base.time;
    opens cn.wjybxx.base.tuple;
}