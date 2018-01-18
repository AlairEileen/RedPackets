
namespace Tools
{
    public enum ActionParams
    {
        /// <summary>
        /// 请求成功
        /// </summary>
        code_ok=1000,
        /// <summary>
        /// 请求失败
        /// </summary>
        code_error =2000,
        /// <summary>
        /// 请求参数不正确
        /// </summary>
        code_error_null = 2001,
        /// <summary>
        /// 数据为空
        /// </summary>
        code_null =3000,
        /// <summary>
        /// 验证失败
        /// </summary>
        code_error_verify =3001,
        /// <summary>
        /// 重复异常
        /// </summary>
        code_error_exists =3002,
        /// <summary>
        /// 余额不足
        /// </summary>
        code_insufficient_balance=4000,
        /// <summary>
        /// 该账户已经领取
        /// </summary>
        packets_people_opened=5000,
        /// <summary>
        /// 红包已经领取完成
        /// </summary>
        packets_opened=5001
    }
  
}
