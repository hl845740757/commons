/**
 * @author wjybxx
 * date - 2024/1/6
 */
module wjybxx.btree.core {
    requires jsr305;
    requires org.slf4j;

    requires transitive wjybxx.commons.base;

    exports cn.wjybxx.btree;
    exports cn.wjybxx.btree.branch;
    exports cn.wjybxx.btree.branch.join;
    exports cn.wjybxx.btree.decorator;
    exports cn.wjybxx.btree.fsm;
    exports cn.wjybxx.btree.leaf;

    opens cn.wjybxx.btree;
    opens cn.wjybxx.btree.branch;
    opens cn.wjybxx.btree.branch.join;
    opens cn.wjybxx.btree.decorator;
    opens cn.wjybxx.btree.fsm;
    opens cn.wjybxx.btree.leaf;
}