/**
 * @author wjybxx
 * date - 2024/5/20
 */
module wjybxx.btree.codec {
    requires jsr305;
    requires static java.compiler; // 生成代码的注解依赖，保留权限为Source
    requires transitive org.slf4j; // codec依赖了slf4j

    requires wjybxx.btree.core;
    requires wjybxx.dson.core;
    requires wjybxx.dson.codec;

    exports cn.wjybxx.btreecodec;
    exports cn.wjybxx.btreecodec.fsm; // 以下目录编译时生成
    exports cn.wjybxx.btreecodec.decorator;
    exports cn.wjybxx.btreecodec.branch;
    exports cn.wjybxx.btreecodec.branch.join;
    exports cn.wjybxx.btreecodec.leaf;

    opens cn.wjybxx.btreecodec;
    opens cn.wjybxx.btreecodec.fsm;
    opens cn.wjybxx.btreecodec.decorator;
    opens cn.wjybxx.btreecodec.branch;
    opens cn.wjybxx.btreecodec.branch.join;
    opens cn.wjybxx.btreecodec.leaf;
}