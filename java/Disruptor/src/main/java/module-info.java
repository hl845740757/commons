/**
 * disruptor模块保持不依赖base，以方便分离为独立项目。
 *
 * @author wjybxx
 * date - 2024/1/18
 */
module wjybxx.commons.disruptor {
    requires jsr305;

    exports cn.wjybxx.disruptor;
    opens cn.wjybxx.disruptor;
}