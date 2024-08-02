/**
 * @author wjybxx
 * date - 2023/12/24
 */
module wjybxx.commons.agent {
    requires jdk.attach;
    requires transitive java.instrument; // 运行时必须

    exports cn.wjybxx.agent;
    opens cn.wjybxx.agent;
}