import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'

export function RegistrationNotice() {
  return (
    <Card>
      <CardHeader>
        <CardTitle>註冊提醒與須知</CardTitle>
        <CardDescription>
          請務必提前下載「數位憑證皮夾 App」，以便順利完成註冊手續
        </CardDescription>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-6">
            <div className="flex flex-col items-center space-y-3">
              <img
                src={`${import.meta.env.BASE_URL}androidQrcode.png`}
                alt="Android QRCode"
                className="w-32 h-32 object-contain"
              />
              <a
                href="https://play.google.com/store/apps/details?id=tw.gov.moda.diw"
                target="_blank"
                rel="noopener noreferrer"
                className="block"
              >
                <img
                  src={`${import.meta.env.BASE_URL}googleplay.png`}
                  alt="Google Play"
                  className="h-10 w-auto object-contain"
                />
              </a>
              <p className="text-xs text-muted-foreground text-center">
                支援 Android 10 以上(含)
              </p>
            </div>
            <div className="flex flex-col items-center space-y-3">
              <img
                src={`${import.meta.env.BASE_URL}iosQrcode.png`}
                alt="iOS QRCode"
                className="w-32 h-32 object-contain"
              />
              <a
                href="https://apps.apple.com/tw/app/%E6%95%B8%E4%BD%8D%E6%86%91%E8%AD%89%E7%9A%AE%E5%A4%BE/id6742222992"
                target="_blank"
                rel="noopener noreferrer"
                className="block"
              >
                <img
                  src={`${import.meta.env.BASE_URL}applestore.png`}
                  alt="App Store"
                  className="h-10 w-auto object-contain"
                />
              </a>
              <p className="text-xs text-muted-foreground text-center">
                支援 iOS 15.0 以上(含)
              </p>
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
