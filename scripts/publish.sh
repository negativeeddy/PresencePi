sudo systemctl stop AspNetSite
dotnet publish -c Release -o /srv/AspNetSite/ ./NegativeeEddy.PresencePi.csproj
sudo cp ./AspNetSite.service /etc/systemd/system/AspNetSite.service
sudo systemctl daemon-reload
sudo systemctl start AspNetSite
