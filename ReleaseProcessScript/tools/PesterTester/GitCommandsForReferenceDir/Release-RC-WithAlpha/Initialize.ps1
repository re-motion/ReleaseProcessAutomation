git checkout -b "support/v2.28" *>$NULL
git commit -m "Commit on Support Branch" --allow-empty *>$NULL

git checkout -b "hotfix/v2.28.1" *>$NULL
git commit -m "Feature-A" --allow-empty *>$NULL

git checkout -b "prelease/v2.28.1-beta.1" *>$NULL
git commit -m "Update metadata to version '2.28.1-beta.1'." --allow-empty *>$NULL
git tag -a "v2.28.1-beta.1" -m "v2.28.1-beta.1" *>$NULL

git checkout "hotfix/v2.28.1" *>$NULL
git merge "prelease/v2.28.1-beta.1" --no-ff *>$NULL
git commit -m "Feature-B" --allow-empy *>$NULL

git checkout -b "release/v2.28.1" *>$NULL
git checkout -b "prerelease/v2.28.1-rc.1" *>$NULL
git commit -m "Commit on prerelease branch" --allow-empty *>$NULL
git tag -a "v2.28.1-rc.1" -m "v2.28.1-rc.1" *>$NULL
git checkout "release/v2.28.1" *>$NULL
git merge "prerelease/v2.28.1-rc.1" --no-ff *>$NULL
