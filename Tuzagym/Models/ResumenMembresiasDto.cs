using System;
using System.Collections.Generic;
using System.Text;


namespace Tuzagym.Models
{
    public class ResumenMembresiasDto
    {
        public int TotalSocios { get; set; }

        public int MembresiasVigentes { get; set; }

        public int ProximasAVencer { get; set; }

        public int MembresiasVencidas { get; set; }
    }
}
