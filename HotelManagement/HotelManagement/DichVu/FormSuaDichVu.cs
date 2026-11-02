namespace HotelManagement.DichVu;

public class FormSuaDichVu : DichVuEditor
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string MaDichVu { get => RecordId ?? ""; set => RecordId = value; }
    public FormSuaDichVu() : base(true) { }
}
