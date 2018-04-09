using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace chrome_push
{
    public class chrome_data
    {
        public int id { set; get; }
        public chrome_result result { set; get; } 
    }

    public class chrome_result
    {
        public chrome_result_item result { set; get; } 
    }

    public class chrome_result_item
    {
        public string type { set; get; }
        public string value { set; get; }
    }
}
