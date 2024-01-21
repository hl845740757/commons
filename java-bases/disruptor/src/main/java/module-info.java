/**
 * @author wjybxx
 * date - 2024/1/18
 */
module wjybxx.commons.disruptor {
    requires jsr305;
    requires transitive wjybxx.commons.base;

    exports cn.wjybxx.disruptor;
    opens cn.wjybxx.disruptor;
}