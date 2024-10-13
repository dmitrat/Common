using MessagePack;
using OutWit.Common.Abstract;

namespace OutWit.Common.MessagePack.Messages
{
    [MessagePackObject]
    public class NetMessage : ModelBase
    {
        #region Contructors

        private NetMessage() :
            this("", false)
        {

        }

        [SerializationConstructor]
        public NetMessage(string message, bool isError)
        {
            Message = message;
            IsError = isError;
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return IsError ? $"ERROR: {Message}" : Message;
        }

        #endregion

        #region Model Base
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is NetMessage message))
                return false;

            return IsError == message.IsError && 
                   Message == message.Message;
        }

        public override ModelBase Clone()
        {
            return new NetMessage(Message, IsError);
        }
        #endregion

        #region Properties

        [Key(0)]
        public string Message { get;}

        [Key(1)]
        public bool IsError { get;}

        #endregion
    }
}
