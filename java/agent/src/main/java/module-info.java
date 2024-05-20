/**
 * @author wjybxx
 * date - 2023/12/24
 */
module wjybxx.commons.agent {
    requires jdk.attach;
    requires java.instrument;

    exports cn.wjybxx.agent;
    opens cn.wjybxx.agent;
}