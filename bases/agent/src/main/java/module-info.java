/**
 * 热更新模块
 *
 * @author wjybxx
 * date - 2023/12/24
 */
module wjybxx.commons.agent {
    requires java.instrument;
    requires jdk.attach;

    exports cn.wjybxx.common;
    opens cn.wjybxx.common;
}