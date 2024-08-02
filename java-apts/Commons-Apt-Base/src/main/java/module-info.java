/**
 * @author wjybxx
 * date - 2024/1/6
 */
module wjybxx.commons.aptbase {
    requires transitive jsr305; // 存在运行时依赖
    requires transitive com.squareup.javapoet; // 基础工具传递下去
    requires transitive java.compiler;

    exports cn.wjybxx.apt;
    opens cn.wjybxx.apt;
}