# КТ № 8. Анализ результатов Petstore

Колосов Иван Максимович, группа 3П3.24.

Оба проекта используют 38 сценариев из `cases.json` с одинаковыми ожиданиями. В матрице 19 операций с позитивной и негативной проверкой для каждой. Устаревший `findByTags` оставлен в полном наборе КТ № 6. В этой работе он исключён, чтобы соблюсти требуемые 38 проверок на фреймворк.

```sh
dotnet test xunit-tests --logger "trx;LogFileName=xunit.trx" --results-directory run
dotnet test nunit-tests --logger "trx;LogFileName=nunit.trx" --results-directory run
```

Для запуска требуется .NET 10 SDK и доступ к `https://petstore.swagger.io/v2/`. Файлы `run/xunit.trx` и `run/nunit.trx` содержат результаты и сообщения об ошибках. Сводка каждого запуска сохранена в папке `run`.

- [Таблицы и анализ](ANALYSIS.md)
- [Контрольные вопросы](QUESTIONS.md)
- [Снимок сводки xUnit](run/xunit.jpg)
- [Снимок сводки NUnit](run/nunit.jpg)
