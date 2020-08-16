## Rss auto crawling using Github Action

**Github Action does all these steps automatically, it run rss crawler every 4 hours**

Steps:
  - Git action pull repository, build and run crawler project (C#)
  - Read channel urls from LiteDB
  - Fetch rss feed items
  - Insert feed items into LiteDB after checking blacklist and existing
  - Generate all rss items to static page (index.html) https://minhhungit.github.io/github-action-rss-crawler/
  - Commit change (litedb database & index.html page)
  - Push
  
## Workflow
```yml
on:
  schedule:
    # Runs every 4h
    - cron: '0 */4 * * *'
  workflow_dispatch:
  
jobs:
  update-readme-with-blog:
    name: Crawl rss and generate static page
    runs-on: windows-2019
    steps:
      - uses: actions/checkout@main
        with:
          repository: minhhungit/github-action-rss-crawler
          token: ${{ secrets.GITHUB_TOKEN }}
      - uses: actions/setup-dotnet@v1
        with:
          dotnet-version: 3.1.x
      #- run: dotnet build DemoApp\DemoApp.sln      
      - run: dotnet run --project RssCrawler\RssCrawler.csproj
      - run: git config --local user.email "it.minhhung@gmail.com"
      - run: git config --local user.name "Jin"
      - run: git add .
      - run: git commit -m "Add changes"
      - run: git push
```

> https://github.com/minhhungit/github-action-rss-crawler/blob/master/.github/workflows/crawl-rss.yml

## Demo
<img src="https://raw.githubusercontent.com/minhhungit/github-action-rss-crawler/master/images/demo.png" />
