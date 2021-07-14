using System.Collections.Generic;
using System.Threading.Tasks;

namespace SqlObjectCopy.DBActions
{
    internal interface IDbAction
    {
        /// <summary>
        /// The next action in the chain
        /// </summary>
        public IDbAction NextAction { get; set; }

        /// <summary>
        /// The entry point for this Action
        /// </summary>
        /// <returns></returns>
        public void Handle(List<SqlObject> objects, Options options);
    }
}
