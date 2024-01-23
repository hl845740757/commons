/**
 * @author wjybxx
 * date - 2024/1/21
 */
module wjybxx.commons.concurrent {
    requires jsr305;
    requires org.slf4j;

    requires wjybxx.commons.base;
    requires wjybxx.commons.disruptor;

    exports cn.wjybxx.concurrent;
    exports cn.wjybxx.unitask;

    opens cn.wjybxx.concurrent;
    opens cn.wjybxx.unitask;
}