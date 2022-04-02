using System.Runtime.Serialization;

namespace Quickstarts.Servers.LiHaSystem
{
    [DataContract(Namespace = Namespaces.LiHa)]
    public class LiHaConfiguration
    {
        #region Constructors
        /// <summary>
        /// The default constructor.
        /// </summary>
        public LiHaConfiguration()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the object during deserialization.
        /// </summary>
        [OnDeserializing()]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }

        /// <summary>
        /// Sets private members to default values.
        /// </summary>
        private static void Initialize()
        {
        }
        #endregion
    }
}
