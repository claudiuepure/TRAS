﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web; 

namespace ViewModels
{
    public class FeatureViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Lat { get; set; }
        public double Long { get; set; }
        public string Code { get; set; }
        public PlaceViewModel Parent { get; set; }
    }
}