using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace SubtitleTranslator
{
    public class Trans_resultItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string src { get; set; }
        /// <summary>
        /// ƻ��
        /// </summary>
        public string dst { get; set; }
    }

    public class resp_str
    {
        /// <summary>
        /// 
        /// </summary>
        public string from_s { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string to_s { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<Trans_resultItem> trans_result { get; set; }
    }
}