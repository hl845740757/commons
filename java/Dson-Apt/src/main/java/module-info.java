import cn.wjybxx.dsonapt.CodecProcessor;

import javax.annotation.processing.Processor;

/**
 * @author wjybxx
 * date - 2024/5/20
 */
module wjybxx.dson.apt {
    requires jsr305;
    requires com.squareup.javapoet;
    requires com.google.auto.service; // 只引入注解，注解处理器在pom中配置

    requires java.compiler;
    requires wjybxx.commons.aptbase;

    exports cn.wjybxx.dsonapt;

    provides Processor with CodecProcessor;
}