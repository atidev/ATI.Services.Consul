# ATI.Services.Consul
## Деплой


### Теги

Выкладка в nuget происходит на основе триггера на тег определённого формата: [как повышать версию](https://learn.microsoft.com/en-us/nuget/concepts/package-versioning)
##### ВАЖНО: 
1. Все теги должны начинаться с `v`
2. Для тестовой версии тег должен быть с постфиксом, например `v1.0.1-rc`
3. Релизный тег должен состоять только из цифр версии, например `v1.0.0`

* Создание тега через git(нужно запушить его в origin) [создание тега и пуш в remote](https://git-scm.com/book/en/v2/Git-Basics-Tagging)
  * Команды для тестового:
    1. `git checkout <название ветки>`
    2. `git tag -a <название тега> -m "<описание тега>" ` 
    3. `git push --tags`
  * Команды для релизного:
    1. `git tag -a <название тега> <SHA коммита> -m "<описание тега>" `
    2. `git push --tags`
* Через раздел [releases](https://github.com/atidev/ATI.Services.Consul/releases)(альфа версии нужно помечать соответсвующей галкой).
* При пуше, в некоторых IDE, необходимо отметить чекбокс об отправке тегов


#### Разработка теперь выглядит вот так:
1. Создаем ветку, пушим изменения, создаем pull request.
2. Добавляем на ветку тег с версией изменения
3. Срабатывает workflow билдит и пушит версию(берёт из названия тега) в nuget.
4. По готовности мерджим ветку в master.
5. Тегаем нужный коммит мастера.
Нужно обязательно описать изменения внесённые этим релизом в release notes
Здесь лучше воспользоваться интерфейсом гитхаба, там удобнее редактировать текст.
6. Срабатывает релизный workflow билдит и пушит в нугет релизную версию.
7. В разделе [Releases](https://github.com/atidev/ATI.Services.Consul/releases) появляется информация о нашем релиз и release notes.

---
## Документация

### Http

За основу взята работа с `HttClientFactory` из `atisu.services.common`, но добавлены следующие extensions:
1. `services.AddConsulHttpClient<XServiceOptions>`
2. `services.AddConsulHttpClients()` - он автоматически соберет из проекта всех наследников `BaseServiceOptions`, где `ConsulName не NULL и UseHttpClientFactory = true` и для них сделает вызов `services.AddConsulHttpClient<>()` 

Методы делают почти все то же самое, что и `services.AddCustomHttpClient<>`, но дополнительно:
1. Добавляется `ServiceAsClientName` хедер во все запросы
2. Добавляется `HttpConsulHandler`, который на каждый запрос (retry) получает ip+port инстанса из `ConsulServiceAddress`

---



