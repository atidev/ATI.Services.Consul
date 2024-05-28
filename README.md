# ATI.Services.Consul
## Деплой
Выкладка в nuget происходит на основе триггера на тег определённого формата
- `v1.0.0` - формат релизная версия на ветке master
- `v1.0.0-rc1` - формат тестовой/альфа/бета версии на любой ветке

Тег можно создать через git(нужно запушить его в origin) [создание тега и пуш в remote](https://git-scm.com/book/en/v2/Git-Basics-Tagging)
или через раздел [releses](https://github.com/atidev/ATI.Services.Consul/releases)(альфа версии нужно помечать соответсвующей галкой).

#### Разработка теперь выглядит вот так:
1. Создаем ветку, пушим изменения, создаем pull request.
2. Вешаем на ветку тег например `v1.0.2-new-auth-12`
3. Срабатывает workflow билдит и пушит версию(берёт из названия тега) в nuget.
4. По готовности мерджим ветку в master.
5. Вешаем релизный тег на нужный коммит мастера.
Нужно обязательно описать изменения внесённые этим релизом в release notes
Здесь лучше воспользоваться интерфейсом гитхаба, там удобнее редакитровать текст.
6. Срабатывает релизный workflow билдит и пушит в нугет релизную версию.
7. В разделе [Releses](https://github.com/atidev/ATI.Services.Consul/releases) появляется информация о нашем релиз и release notes.

## Документация

### Consul
Чтобы зарегистрировать сервис в консуле нужно:
`в appsettings.json` поместить блок
```json
 "ConsulRegistratorOptions": {
    "ProvideEnvironment": "env",
    "ConsulServiceOptions": [
      {
        "ServiceName": "your-service-name",
        "Tags": ["env"],
        "Check": {
          "HTTP": "/api/health/service",
          "DeregisterCriticalServiceAfter": "00:00:05",
          "Interval": "00:00:05",
          "Timeout": "00:00:02"
        }
      }
    ]
 }
```
> Да, там массив. Да, можно зарегать один сервак под разными тегами, именами, всем.
> Это необходимо в сервиса [нотификаций](http://stash.ri.domain:7990/projects/AS/repos/ati.notifications.core/browse/ATI.Notifications.Core.API/appsettings.json), для версионирования старых мобильных приложений, которые ходят без прокси и не выживают после смены контрактов.

Далее осталось только добавить в `Startup.cs` `services.AddConsul()`

---

### Http

За основу взята работа с `HttClientFactory` из `atisu.services.common`, но добавлены следующие extensions:
1. `services.AddConsulHttpClient<TAdapter, TServiceOptions>`

Методы делают почти все то же самое, что и `services.AddCustomHttpClient<>`, но дополнительно:
1. Добавляется `ServiceAsClientName` хедер во все запросы
2. Добавляется `HttpConsulHandler`, который на каждый запрос (retry) получает ip+port инстанса из `ConsulServiceAddress`

---

