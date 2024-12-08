using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustyBase.Common.Contracts;

public interface IMessageForUserTools
{
    public void ShowSimpleMessageBoxInstance(Exception ex);
    public void ShowSimpleMessageBoxInstance(string messageForUser, string title = "Information");
    public void FlashWindowExIfNeeded();
}
