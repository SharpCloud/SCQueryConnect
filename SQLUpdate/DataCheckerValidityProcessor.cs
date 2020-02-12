using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Interfaces;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace SCQueryConnect
{
    public class DataCheckerValidityProcessor : IDataCheckerValidityProcessor
    {
        private readonly IMainViewModel _viewModel;
        private readonly Expression<Func<IMainViewModel, bool>> _selector;

        public DataCheckerValidityProcessor(
            IMainViewModel viewModel,
            Expression<Func<IMainViewModel, bool>> selector)
        {
            _viewModel = viewModel;
            _selector = selector;
        }

        public void ProcessDataValidity(bool isOk)
        {
            var prop = (PropertyInfo)((MemberExpression)_selector.Body).Member;
            prop.SetValue(_viewModel, isOk, null);
        }
    }
}
