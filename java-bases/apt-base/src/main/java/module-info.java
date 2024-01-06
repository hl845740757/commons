/**
 * @author wjybxx
 * date - 2024/1/6
 */
module wjybxx.commons.aptbase {
    requires jsr305;
    requires com.squareup.javapoet;

    requires java.compiler;
    requires wjybxx.commons.base;

    exports cn.wjybxx.apt;
    opens cn.wjybxx.apt;
}