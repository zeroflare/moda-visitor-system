import {
  Card,
  CardContent,
  CardDescription,
  CardTitle,
} from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import { Clock, QrCode, ExternalLink } from 'lucide-react'

interface QRCodeSelectionProps {
  qrcodeImage: string
  qrcodeExpiry: number
  authUri: string
}

export function QRCodeSelection({
  qrcodeImage,
  qrcodeExpiry,
  authUri,
}: QRCodeSelectionProps) {
  return (
    <Card>
      <CardContent className="pt-6">
        <div className="mb-4 text-center">
          <p className="text-sm text-muted-foreground">
            請選擇以下任一方式完成註冊
          </p>
        </div>
        <div className="grid gap-6 md:grid-cols-[1fr_auto_1fr] items-stretch">
          <div className="flex flex-col items-center justify-center space-y-4 p-8 bg-muted/50 rounded-lg min-h-[400px]">
            <div className="relative">
              <img
                src={qrcodeImage}
                alt="註冊 QRCode"
                className="w-full max-w-[280px] h-auto rounded-lg border-2 border-border shadow-lg"
              />
              {qrcodeExpiry > 0 && (
                <Badge
                  variant={qrcodeExpiry < 60 ? 'destructive' : 'warning'}
                  className="absolute -bottom-3 left-1/2 -translate-x-1/2"
                >
                  <Clock className="h-3 w-3 mr-1" />
                  {Math.floor(qrcodeExpiry / 60)}:
                  {(qrcodeExpiry % 60).toString().padStart(2, '0')}
                </Badge>
              )}
            </div>
            <div className="text-center space-y-2">
              <CardTitle className="flex items-center justify-center gap-2 text-xl">
                <QrCode className="h-5 w-5" />
                掃描 QRCode
              </CardTitle>
              <CardDescription>
                使用數位憑證皮夾 App 掃描上方 QRCode
              </CardDescription>
            </div>
          </div>
          <div className="flex flex-col items-center justify-center px-4 hidden md:flex">
            <div className="flex-1 w-px border-l-2 border-dashed border-muted-foreground/30"></div>
            <div className="px-3 py-2 bg-background border border-muted-foreground/30 rounded-full text-muted-foreground text-sm font-medium">
              或
            </div>
            <div className="flex-1 w-px border-l-2 border-dashed border-muted-foreground/30"></div>
          </div>
          <div className="flex items-center justify-center py-4 md:hidden">
            <Separator className="w-full" />
            <div className="px-3 text-muted-foreground text-sm font-medium">
              或
            </div>
            <Separator className="w-full" />
          </div>
          <div className="flex flex-col items-center justify-center space-y-4 p-8 bg-muted/50 rounded-lg min-h-[400px]">
            {authUri && (
              <>
                <div className="flex items-center justify-center w-20 h-20 rounded-full bg-primary/10 mb-4">
                  <ExternalLink className="h-10 w-10 text-primary" />
                </div>
                <div className="text-center space-y-2 mb-4">
                  <CardTitle className="text-xl">點擊連結</CardTitle>
                  <CardDescription>直接開啟數位憑證皮夾 App</CardDescription>
                </div>
                <Button
                  onClick={() => window.open(authUri, '_blank')}
                  size="lg"
                  className="w-full max-w-[280px]"
                >
                  <ExternalLink className="h-4 w-4 mr-2" />
                  前往數位憑證皮夾
                </Button>
              </>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
