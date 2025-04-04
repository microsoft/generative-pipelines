# Adding SK sources, for local patching:

    cd repo_root

    git submodule add -b generative-pipelines https://github.com/dluc/semantic-kernel.git tools/_libs/SK

    git submodule add -b feature-vector-data-preb2 https://github.com/microsoft/semantic-kernel.git tools/_libs/SK

    git submodule update --init --recursive

# Pulling updates

    cd tools/_libs/SK

    git pull origin main

    cd ../../../

    git add tools/_libs/SK
    git commit -m "Update SK submodule"

# Deleting the submodule

    cd repo_root

    git submodule deinit -f tools/_libs/SK
    
    rm -rf .git/modules/tools/_libs/SK
    rm -rf tools/_libs/SK

    git rm -f tools/_libs/SK
    git commit -m "Remove SK submodule"
