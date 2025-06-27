using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Abstract;

namespace OutWit.Common.Tests.Mock
{
    public class PropertyTestModel : ModelBase
    {
        public string m_publicField = "Initial Field";


        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            throw new NotImplementedException();
        }

        public override ModelBase Clone()
        {
            return new PropertyTestModel
            {
                PublicProperty = this.PublicProperty,
                PrivateSetterProperty = this.PrivateSetterProperty,
                m_publicField = this.m_publicField
            };
        }

        public string GetPrivateSetterValue() => PrivateSetterProperty;

        public void SetPrivatePropertyForTest(string value)
        {
            PrivateSetterProperty = value;
        }

        public string PublicProperty { get; set; } = "Initial";

        public string PrivateSetterProperty { get; private set; } = "Initial";

        public string GetOnlyProperty => "Read-Only";

    }
}
