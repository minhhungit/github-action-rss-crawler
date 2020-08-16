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
  
## Demo
<img src="https://raw.githubusercontent.com/minhhungit/github-action-rss-crawler/master/images/demo.png" />
