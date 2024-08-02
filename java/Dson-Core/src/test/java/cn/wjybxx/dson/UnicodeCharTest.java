package cn.wjybxx.dson;

import org.junit.jupiter.api.Assertions;

/**
 * @author wjybxx
 * date - 2023/7/1
 */
public class UnicodeCharTest {

    //    @Test
    void test() {
        for (int i = 0; i < 65536; i++) {
            char c = (char) i;
            String s1 = encode(c);
            String s2 = encodeFast(c);
            Assertions.assertEquals(s1, s2);
            Assertions.assertEquals(c, decode(s2));
        }
    }

    private static char decode(String hexString) {
        return (char) Integer.parseInt(hexString, 2, hexString.length(), 16);
    }

    private static String encode(char c) {
        return new StringBuilder(6)
                .append("\\u")
                .append(Integer.toHexString((c & 0xF000) >> 12))
                .append(Integer.toHexString((c & 0x0F00) >> 8))
                .append(Integer.toHexString((c & 0x00F0) >> 4))
                .append(Integer.toHexString(c & 0x000F))
                .toString();
    }

    private static String encodeFast(char c) {
        return new StringBuilder(6)
                .append("\\u")
                .append(Integer.toHexString(0X10000 + (int) c), 1, 5)
                .toString();
    }
}