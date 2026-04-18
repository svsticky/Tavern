#!/bin/bash
if ! awslocal s3api head-bucket --bucket profile-pictures 2>/dev/null; then
    awslocal s3 mb s3://profile-pictures
fi

if ! awslocal s3api head-bucket --bucket posters 2>/dev/null; then
    awslocal s3 mb s3://posters
fi

if ! awslocal s3api head-bucket --bucket group-pictures 2>/dev/null; then
    awslocal s3 mb s3://group-pictures
fi