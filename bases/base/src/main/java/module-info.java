/**
 * 可以被个人所有开源项目依赖的的基础模块
 *
 * @author wjybxx
 * date - 2023/12/24
 */
module wjybxx.commons.base {
    requires jsr305;
    exports cn.wjybxx.base;
    opens cn.wjybxx.base;
}