/*
 * Copyright 2024 wjybxx(845740757@qq.com)
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
package cn.wjybxx.btree.leaf;

import cn.wjybxx.btree.LeafTask;
import cn.wjybxx.btree.TaskStatus;

import javax.annotation.Nonnull;
import java.util.Random;
import java.util.random.RandomGenerator;

/**
 * 简单随机任务
 * 在正式的项目中，Random应当从实体上获取。
 *
 * @author wjybxx
 * date - 2023/11/26
 */
public class SimpleRandom<T> extends LeafTask<T> {

    /** 允许指定自己的random */
    public static RandomGenerator random = new Random();

    private float p;

    public SimpleRandom() {
        p = 0.5f;
    }

    public SimpleRandom(float p) {
        this.p = p;
    }

    @Override
    protected void execute() {
        if (random.nextFloat() <= p) {
            setSuccess();
        } else {
            setFailed(TaskStatus.ERROR);
        }
    }

    @Override
    protected void onEventImpl(@Nonnull Object event) {

    }

    public float getP() {
        return p;
    }

    public void setP(float p) {
        this.p = p;
    }
}