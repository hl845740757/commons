/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package cn.wjybxx.dson;

import cn.wjybxx.base.ThreadUtils;
import cn.wjybxx.base.time.StopWatch;
import cn.wjybxx.dson.text.DsonTextReader;
import cn.wjybxx.dson.text.DsonTextReaderSettings;
import cn.wjybxx.dson.text.DsonTextWriter;
import cn.wjybxx.dson.text.DsonTextWriterSettings;

import java.io.*;

/**
 * 大文件读写测试
 * <p>
 * 同样是540K的Json文件，设置于C#的Dson一致，
 * C#   read 35ms, write 10ms
 * java read 76ms, write 42ms...
 *
 * @author wjybxx
 * date - 2024/1/3
 */
public class BigFileTest {

    private static final String inputFilePath = "D:\\Test.json";
    private static final String outputFilePath = "D:\\Test2.json";

    public static void main(String[] args) throws IOException {
        if (!new File(inputFilePath).exists()) {
            return;
        }
        testDson();

        ThreadUtils.sleepQuietly(1000);
        testBson();
    }

    private static File NewInputStream() {
        return new File(inputFilePath);
    }

    private static File NewOutputStream() throws IOException {
        File file = new File(outputFilePath);
        if (!file.exists()) {
            file.createNewFile();
        }
        return file;
    }

    private static void testDson() throws IOException {
        StopWatch stopWatch = StopWatch.createStarted("Wjybxx.Dson");
        DsonValue dsonValue;
        try (DsonTextReader reader = new DsonTextReader(DsonTextReaderSettings.DEFAULT, new FileReader(NewInputStream()))) {
            dsonValue = Dsons.readTopDsonValue(reader);
            stopWatch.logStep("Read");
        }

        DsonTextWriterSettings settings = DsonTextWriterSettings.newBuilder()
                .setEnableText(false)
                .setMaxLengthOfUnquoteString(0)
                .build();
        try (DsonTextWriter writer = new DsonTextWriter(settings, new FileWriter(NewOutputStream()))) {
            Dsons.writeTopDsonValue(writer, dsonValue);
            stopWatch.stop("Write");
            System.out.println(stopWatch.getLog());
        }
    }

    private static void testBson() throws FileNotFoundException {
//        StopWatch stopWatch = StopWatch.createStarted("MongoDB.Bson");
    }
}
