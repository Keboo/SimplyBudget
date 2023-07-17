using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SimplyBudget.ViewModels;

public abstract class ValidationViewModel : ObservableObject, INotifyDataErrorInfo
{
    private interface IValidator
    {
        IEnumerable<object> Validate(ValidationViewModel viewModel);
    }

    private class ReflectionValidator : IValidator
    {
        public PropertyInfo Property { get; }

        public ReflectionValidator(PropertyInfo property)
        {
            Property = property;
        }

        public IEnumerable<object> Validate(ValidationViewModel viewModel)
        {
            foreach (var attribute in Property.GetCustomAttributes<ValidationAttribute>())
            {
                if (!attribute.IsValid(Property.GetValue(viewModel)))
                {
                    yield return attribute.FormatErrorMessage(Property.Name);
                }
            }
        }
    }

    private class DelegateValidator : IValidator
    {
        public DelegateValidator(Func<IEnumerable<object>> validate)
        {
            Validate = validate;
        }

        public Func<IEnumerable<object>> Validate { get; }

        IEnumerable<object> IValidator.Validate(ValidationViewModel _) => Validate();
    }

    private readonly Dictionary<string, IValidator> _ValidatorsByName;
    private readonly Dictionary<string, IList<object>> _ValidationErrorsByProperty = new();

    protected ValidationViewModel()
    {
        _ValidatorsByName = GetType().GetProperties()
            .Select(x => new ReflectionValidator(x))
            .ToDictionary(x => x.Property.Name, x => (IValidator)x);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        ValidateProperty(e.PropertyName ?? "");
    }

    protected bool ValidateModel()
    {
        bool rv = true;
        foreach (string propertyName in _ValidatorsByName.Keys)
        {
            rv &= ValidateProperty(propertyName);
        }
        return rv;
    }

    protected bool ValidateProperty(string propertyName)
    {
        if (_ValidatorsByName.TryGetValue(propertyName, out IValidator? validator))
        {
            var errors = validator.Validate(this).ToList();

            if (errors.Any())
            {
                _ValidationErrorsByProperty[propertyName] = errors;
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
                return false;
            }
            if (_ValidationErrorsByProperty.Remove(propertyName))
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }

        return true;
    }

    protected void SetValidationErrors(string propertyName, IEnumerable<object> errors)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException($"'{nameof(propertyName)}' cannot be null or whitespace", nameof(propertyName));
        }

        _ValidationErrorsByProperty[propertyName] = errors?.ToList() ?? (IList<object>)Array.Empty<object>();
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    protected void ClearValidationErrors(string propertyName)
        => SetValidationErrors(propertyName, Array.Empty<object>());

    protected void AddValidator(string propertyName, Func<IEnumerable<object>> validate)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException($"'{nameof(propertyName)}' cannot be null or whitespace", nameof(propertyName));
        }

        if (validate is null)
        {
            throw new ArgumentNullException(nameof(validate));
        }

        _ValidatorsByName[propertyName] = new DelegateValidator(validate);
    }

    public IEnumerable GetErrors(string? propertyName)
    {
        if (_ValidationErrorsByProperty.TryGetValue(propertyName ?? "", out IList<object>? errors))
        {
            return errors;
        }
        return Array.Empty<object>();
    }

    public bool HasErrors => _ValidationErrorsByProperty.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
}
