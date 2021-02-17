## Rss auto crawling using Github Action

**Github Action does all these steps automatically, it run rss crawler every 4 hours**

Steps:
  - Github will pull repository, build and run crawler code (crawling code is C# (.net core), github will run it directly)
  - Read channel urls from LiteDB
  - Fetch rss feed items
  - Insert feed items into LiteDB after checking blacklist and existing
  - Generate all rss items to static page (index.html - https://minhhungit.github.io/github-action-rss-crawler/ )
  - Commit change (litedb database & index.html page) and push to this repo

  
### Workflow
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

---

### Demo

> https://minhhungit.github.io/github-action-rss-crawler/

<img src="https://raw.githubusercontent.com/minhhungit/github-action-rss-crawler/master/images/demo.png" />


### Donate ^^
**If you like my works and would like to support then you can buy me a coffee ☕️ anytime**

<a href='https://ko-fi.com/I2I13GAGL' target='_blank'><img height='36' style='border:0px;height:36px;' src='https://cdn.ko-fi.com/cdn/kofi4.png?v=2' border='0' alt='Buy Me a Coffee at ko-fi.com' /></a> 

**I would appreciate it!!!**
