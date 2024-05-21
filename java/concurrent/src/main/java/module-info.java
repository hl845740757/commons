/**
 * @author wjybxx
 * date - 2024/1/21
 */
module wjybxx.commons.concurrent {
    requires jsr305;
    requires transitive org.slf4j;

    requires transitive wjybxx.commons.base;
    requires transitive wjybxx.commons.disruptor;

    exports cn.wjybxx.concurrent;
    exports cn.wjybxx.sequential;

    opens cn.wjybxx.concurrent;
    opens cn.wjybxx.sequential;
}