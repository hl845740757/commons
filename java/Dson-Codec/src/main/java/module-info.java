/**
 * 默认全部导出
 *
 * @author wjybxx
 * date - 2023/12/24
 */
module wjybxx.dson.codec {
    requires jsr305;
    requires it.unimi.dsi.fastutil.core;
    requires static java.compiler; // 测试用例依赖
    requires transitive org.slf4j;

    requires transitive wjybxx.commons.base;
    requires transitive wjybxx.dson.core;

    exports cn.wjybxx.dsoncodec;
    exports cn.wjybxx.dsoncodec.codecs;
    exports cn.wjybxx.dsoncodec.annotations;

    opens cn.wjybxx.dsoncodec;
    opens cn.wjybxx.dsoncodec.codecs;
    opens cn.wjybxx.dsoncodec.annotations;
}