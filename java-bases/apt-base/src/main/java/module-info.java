/**
 * @author wjybxx
 * date - 2024/1/6
 */
module wjybxx.commons.aptbase {
    requires jsr305;
    requires com.squareup.javapoet;

    requires transitive java.compiler;
    requires transitive wjybxx.commons.base;

    exports cn.wjybxx.apt;
    opens cn.wjybxx.apt;
}