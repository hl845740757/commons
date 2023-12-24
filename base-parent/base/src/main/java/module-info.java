/**
 * 可以被个人所有开源项目依赖的的基础模块
 *
 * @author wjybxx
 * date - 2023/12/24
 */
module wjybxx.commons.base {
    requires jsr305;

    exports cn.wjybxx.common;
    exports cn.wjybxx.common.annotation;
    exports cn.wjybxx.common.collection;
    exports cn.wjybxx.common.ex;
    exports cn.wjybxx.common.pool;
    exports cn.wjybxx.common.reflect;
    exports cn.wjybxx.common.time;

    opens cn.wjybxx.common;
    opens cn.wjybxx.common.annotation;
    opens cn.wjybxx.common.collection;
    opens cn.wjybxx.common.ex;
    opens cn.wjybxx.common.pool;
    opens cn.wjybxx.common.reflect;
    opens cn.wjybxx.common.time;
}