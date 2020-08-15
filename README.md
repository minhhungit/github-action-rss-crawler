## Rss Cralwer using Github Action

Steps:
  - Build project
  - Read channel url from LiteDB
  - Fetch rss feed item and insert into LiteDB (10 newest items)
  - Generate rss item viewer static page https://minhhungit.github.io/github-action-rss-crawler/
  - Commit change
  - Push
  
Github Action does all above steps, it will run rss crawler every 2 hours