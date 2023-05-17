namespace AppSugerenciasUI.Components;
using Microsoft.AspNetCore.Components.Forms;
public class MyInputRadioGroup<TValue> : InputRadioGroup<TValue>
{
    private string _nombre;
    private string _fieldClass;

    protected override void OnParametersSet()
    {
        var fieldClass = EditContext?.FieldCssClass(FieldIdentifier) ?? string.Empty;
        if(fieldClass != _fieldClass || Name!= _nombre) 
        {
            _fieldClass = fieldClass;
            _nombre = Name;
            base.OnParametersSet();
        }
    }
}
