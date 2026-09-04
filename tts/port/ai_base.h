/* ai_base.h (port) — adapted from common/ai_base.h for nncase 2.11.
 * Only change vs vendor: get_output() keeps the to_host() tensors and
 * their map buffers alive in members (2.11 returns a fresh host tensor
 * from to_host(); letting it die frees the data the demos point at). */
#ifndef AI_BASE_H
#define AI_BASE_H

#include <vector>
#include <string>
#include <fstream>

#include <nncase/runtime/interpreter.h>
#include <nncase/runtime/runtime_op_utility.h>
#include <nncase/runtime/util.h>
#include <nncase/runtime/simple_types.h>
#include <nncase/runtime/runtime_tensor.h>
#include "scoped_timing.hpp"

using std::string;
using std::vector;
using namespace nncase::runtime;

/**
 * @brief AI基类，封装nncase相关操作
 * 主要封装了nncase的加载、设置输入、运行、获取输出操作，后续开发demo只需要关注模型的前处理、后处理即可
 */
class AIBase
{
public:
    /**
     * @brief AI基类构造函数，加载kmodel,并初始化kmodel输入、输出
     * @param kmodel_file kmodel文件路径
     * @param debug_mode  0（不调试）、 1（只显示时间）、2（显示所有打印信息）
     * @return None
     */
    AIBase(const char *kmodel_file,const string model_name, const int debug_mode = 1);

    /**
     * @brief AI基类析构函数
     * @return None
     */
    ~AIBase();
    
     /**
     * @brief 根据索引获取kmodel输入tensor
     * @param idx 输入数据指针
     * @param tensor 输入tensor
     * @return None
     */
    void set_input_tensor(size_t idx, runtime_tensor &tensor);

    /**
     * @brief 根据索引获取kmodel输入tensor
     * @param idx 输入数据指针
     * @return None
     */
    runtime_tensor get_input_tensor(size_t idx);

    /**
     * @brief 推理kmodel
     * @return None
     */
    void run();

    /**
     * @brief 获取kmodel输出，结果保存在对应的类属性中
     * @return None
     */
    void get_output();

protected:
    string model_name_;                    // 模型名字
    int debug_mode_;                       // 调试模型，0（不打印），1（打印时间），2（打印所有）
    vector<float *> p_outputs_;            // kmodel输出对应的指针列表
    vector<vector<int>> input_shapes_;     //{{N,C,H,W},{N,C,H,W}...}
    vector<vector<int>> output_shapes_;    //{{N,C,H,W},{N,C,H,W}...}} 或 {{N,C},{N,C}...}}等
    vector<int> each_input_size_by_byte_;  //{0,layer1_length,layer1_length+layer2_length,...}
    vector<int> each_output_size_by_byte_; //{0,layer1_length,layer1_length+layer2_length,...}
private:
    /**
     * @brief 首次初始化kmodel输入，并获取输入shape
     * @return None
     */
    void set_input_init();

    /**
     * @brief 首次初始化kmodel输出，并获取输出shape
     * @return None
     */
    void set_output_init();

    interpreter kmodel_interp_;        // kmodel解释器，从kmodel文件构建，负责模型的加载、输入输出设置和推理
    vector<unsigned char> kmodel_vec_; // 通过读取kmodel文件得到整个kmodel数据，用于传给kmodel解释器加载kmodel

    // port addition: keep the 2.11 to_host() results (and their map
    // views) alive as long as p_outputs_ pointers are being read.
    vector<runtime_tensor> host_outs_;
    vector<mapped_buffer> mapped_outs_;
};
#endif
