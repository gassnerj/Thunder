namespace ThunderApp.Services;

public interface ISettingsService<T>
{
    T Load();
    void Save(T settings);
}