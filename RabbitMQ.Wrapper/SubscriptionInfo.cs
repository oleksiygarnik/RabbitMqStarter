using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQ.Wrapper
{
    public class SubscriptionInfo
    {
		public Type HandlerType { get; }

		private SubscriptionInfo(Type handlerType)
		{
			HandlerType = handlerType ?? throw new ArgumentNullException(nameof(handlerType));
		}

		public static SubscriptionInfo Typed(Type handlerType)
		{
			return new SubscriptionInfo(handlerType);
		}
	}
}
